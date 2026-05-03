using Microsoft.Data.Sqlite;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Read config.json for DB path
string dbPath = "PharmaDB.sqlite";
string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
if (!File.Exists(configPath)) configPath = "config.json";
if (File.Exists(configPath))
{
    var cfg = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(configPath));
    if (cfg.TryGetProperty("dbPath", out var dp)) dbPath = dp.GetString() ?? dbPath;
}

// Make dbPath absolute relative to exe location
if (!Path.IsPathRooted(dbPath))
    dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbPath);

string connStr = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared;";

// ─── DB Helper ────────────────────────────────────────────────────────────────
SqliteConnection Open()
{
    var conn = new SqliteConnection(connStr);
    conn.Open();
    conn.Execute("PRAGMA journal_mode=WAL;");
    return conn;
}

// ─── LISTEN on all interfaces (LAN) ──────────────────────────────────────────
builder.WebHost.UseUrls("http://0.0.0.0:5000");
var app = builder.Build();

// ─── CORS (allow WPF app from any origin) ────────────────────────────────────
app.Use(async (ctx, next) => {
    ctx.Response.Headers.Append("Access-Control-Allow-Origin", "*");
    if (ctx.Request.Method == "OPTIONS") { ctx.Response.StatusCode = 204; return; }
    await next();
});

// ════════════════════════════════════════════════════════════════════
// AUTH
// ════════════════════════════════════════════════════════════════════
app.MapPost("/api/auth/login", async (HttpContext ctx) =>
{
    var body = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    string username = body.GetStringVal("username");
    string password = body.GetStringVal("password");
    string hashedInput = HashPassword(password);

    using var conn = Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Username, Role FROM Users WHERE Username=@u AND (Password=@p OR Password=@ph)";
    cmd.Parameters.AddWithValue("@u", username);
    cmd.Parameters.AddWithValue("@p", password);       // plain (legacy)
    cmd.Parameters.AddWithValue("@ph", hashedInput);   // hashed (new)
    using var reader = cmd.ExecuteReader();
    if (reader.Read())
        return Results.Ok(new { success = true, role = reader.GetString(1) });
    return Results.Ok(new { success = false, role = "" });
});

app.MapPost("/api/auth/change-password", async (HttpContext ctx) =>
{
    var body = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    string username = body.GetStringVal("username");
    string newPassword = HashPassword(body.GetStringVal("newPassword"));
    using var conn = Open();
    conn.Execute("UPDATE Users SET Password=@p WHERE Username=@u",
        ("@p", newPassword), ("@u", username));
    return Results.Ok(new { success = true });
});

// ════════════════════════════════════════════════════════════════════
// DASHBOARD
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/dashboard", () =>
{
    using var conn = Open();
    string today = DateTime.Now.ToString("yyyy-MM-dd");

    double dailySales = conn.Scalar<double>($"SELECT COALESCE(SUM(NetPaid),0) FROM Sales WHERE date(SaleDate)='{today}'");
    long totalMedicines = conn.Scalar<long>("SELECT COUNT(*) FROM Medicines");
    long lowStock = conn.Scalar<long>(@"SELECT COUNT(*) FROM Medicines m
        WHERE COALESCE((SELECT SUM(Quantity) FROM Stocks WHERE MedicineID=m.MedicineID),0) <= m.MinStock");
    long expired = conn.Scalar<long>($"SELECT COUNT(*) FROM Stocks WHERE ExpiryDate < '{today}' AND ExpiryDate != ''");

    var recentSales = conn.Query(@"SELECT s.SaleID, COALESCE(c.Name,'Walk-in Customer') as CustomerName,
        s.NetPaid, s.Status, s.SaleDate
        FROM Sales s LEFT JOIN Customers c ON s.CustomerID=c.CustomerID
        ORDER BY s.SaleID DESC LIMIT 10");

    return Results.Ok(new { dailySales, totalMedicines, lowStock, expired, recentSales });
});

// ════════════════════════════════════════════════════════════════════
// MEDICINES
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/medicines", () =>
{
    using var conn = Open();
    return Results.Ok(conn.Query(@"SELECT m.*, COALESCE((SELECT SUM(Quantity) FROM Stocks WHERE MedicineID=m.MedicineID),0) as TotalStock
        FROM Medicines m ORDER BY m.Name"));
});

app.MapPost("/api/medicines", async (HttpContext ctx) =>
{
    var b = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    using var conn = Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = @"INSERT INTO Medicines (Name,Type,Category,Manufacturer,Unit,PurchasePrice,SalePrice,MinStock,Status)
        VALUES (@n,@t,@cat,@man,@u,@pp,@sp,@ms,'Active'); SELECT last_insert_rowid();";
    cmd.Parameters.AddWithValue("@n", b.GetStringVal("name"));
    cmd.Parameters.AddWithValue("@t", b.GetStringVal("type"));
    cmd.Parameters.AddWithValue("@cat", b.GetStringVal("category"));
    cmd.Parameters.AddWithValue("@man", b.GetStringVal("manufacturer"));
    cmd.Parameters.AddWithValue("@u", b.GetStringVal("unit"));
    cmd.Parameters.AddWithValue("@pp", b.GetDoubleVal("purchasePrice"));
    cmd.Parameters.AddWithValue("@sp", b.GetDoubleVal("salePrice"));
    cmd.Parameters.AddWithValue("@ms", b.GetIntVal("minStock", 10));
    long id = (long)(cmd.ExecuteScalar() ?? 0L);
    return Results.Ok(new { id });
});

app.MapPut("/api/medicines/{id}", async (int id, HttpContext ctx) =>
{
    var b = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    using var conn = Open();
    conn.Execute(@"UPDATE Medicines SET Name=@n,Type=@t,Category=@cat,Manufacturer=@man,
        Unit=@u,PurchasePrice=@pp,SalePrice=@sp,MinStock=@ms WHERE MedicineID=@id",
        ("@n", b.GetStringVal("name")), ("@t", b.GetStringVal("type")),
        ("@cat", b.GetStringVal("category")), ("@man", b.GetStringVal("manufacturer")),
        ("@u", b.GetStringVal("unit")), ("@pp", b.GetDoubleVal("purchasePrice")),
        ("@sp", b.GetDoubleVal("salePrice")), ("@ms", b.GetIntVal("minStock", 10)), ("@id", id));
    return Results.Ok(new { success = true });
});

app.MapDelete("/api/medicines/{id}", (int id) =>
{
    using var conn = Open();
    conn.Execute("DELETE FROM Medicines WHERE MedicineID=@id", ("@id", id));
    return Results.Ok(new { success = true });
});

// ════════════════════════════════════════════════════════════════════
// STOCK
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/stock", () =>
{
    using var conn = Open();
    return Results.Ok(conn.Query(@"SELECT s.*,m.Name as MedicineName FROM Stocks s
        JOIN Medicines m ON s.MedicineID=m.MedicineID ORDER BY s.StockID DESC"));
});

app.MapGet("/api/stock/low", () =>
{
    using var conn = Open();
    return Results.Ok(conn.Query(@"SELECT m.Name, COALESCE(SUM(s.Quantity),0) as TotalQty, m.MinStock
        FROM Medicines m LEFT JOIN Stocks s ON m.MedicineID=s.MedicineID
        GROUP BY m.MedicineID HAVING TotalQty <= m.MinStock"));
});

app.MapGet("/api/stock/expired", () =>
{
    string today = DateTime.Now.ToString("yyyy-MM-dd");
    using var conn = Open();
    return Results.Ok(conn.Query($@"SELECT s.*,m.Name as MedicineName FROM Stocks s
        JOIN Medicines m ON s.MedicineID=m.MedicineID
        WHERE s.ExpiryDate < '{today}' AND s.ExpiryDate != '' AND s.Quantity > 0"));
});

// ════════════════════════════════════════════════════════════════════
// SALES
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/sales", () =>
{
    using var conn = Open();
    return Results.Ok(conn.Query(@"SELECT s.*,COALESCE(c.Name,'Walk-in Customer') as CustomerName
        FROM Sales s LEFT JOIN Customers c ON s.CustomerID=c.CustomerID ORDER BY s.SaleID DESC LIMIT 100"));
});

app.MapGet("/api/sales/today", () =>
{
    string today = DateTime.Now.ToString("yyyy-MM-dd");
    using var conn = Open();
    var sales = conn.Query($@"SELECT s.*,COALESCE(c.Name,'Walk-in Customer') as CustomerName
        FROM Sales s LEFT JOIN Customers c ON s.CustomerID=c.CustomerID
        WHERE date(s.SaleDate)='{today}' ORDER BY s.SaleID DESC");
    double total = conn.Scalar<double>($"SELECT COALESCE(SUM(NetPaid),0) FROM Sales WHERE date(SaleDate)='{today}'");
    return Results.Ok(new { sales, total });
});

app.MapPost("/api/sales", async (HttpContext ctx) =>
{
    var b = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    using var conn = Open();
    using var tx = conn.BeginTransaction();
    try
    {
        // 1. Customer
        string custName = b.GetStringVal("cashPartyName");
        if (string.IsNullOrWhiteSpace(custName)) custName = "Walk-in Customer";
        long custId = conn.Scalar<long>($"SELECT CustomerID FROM Customers WHERE Name='{custName.Replace("'","''")}' LIMIT 1");
        if (custId == 0)
        {
            var cCmd = conn.CreateCommand();
            cCmd.Transaction = tx;
            cCmd.CommandText = "INSERT INTO Customers (Name,Contact) VALUES (@n,''); SELECT last_insert_rowid();";
            cCmd.Parameters.AddWithValue("@n", custName);
            custId = (long)(cCmd.ExecuteScalar() ?? 0L);
        }

        // 2. Insert Sale
        var sCmd = conn.CreateCommand(); sCmd.Transaction = tx;
        sCmd.CommandText = @"INSERT INTO Sales (CustomerID,SaleDate,TotalAmount,Discount,Tax,NetPaid,Status)
            VALUES (@cid,@dt,@tot,@disc,@tax,@net,@st); SELECT last_insert_rowid();";
        sCmd.Parameters.AddWithValue("@cid", custId);
        sCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sCmd.Parameters.AddWithValue("@tot", b.GetDoubleVal("grossTotal"));
        sCmd.Parameters.AddWithValue("@disc", b.GetDoubleVal("discountPercent"));
        sCmd.Parameters.AddWithValue("@tax", b.GetDoubleVal("taxAmount"));
        sCmd.Parameters.AddWithValue("@net", b.GetDoubleVal("grandTotal"));
        sCmd.Parameters.AddWithValue("@st", b.GetStringVal("status", "Paid"));
        long saleId = (long)(sCmd.ExecuteScalar() ?? 0L);

        // 3. Items + FIFO stock deduction
        if (b.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                var dCmd = conn.CreateCommand(); dCmd.Transaction = tx;
                dCmd.CommandText = @"INSERT INTO SaleDetails (SaleID,MedicineID,BatchNo,RackNo,Quantity,UnitPrice,TotalPrice)
                    VALUES (@sid,@mid,@batch,@box,@qty,@price,@total)";
                dCmd.Parameters.AddWithValue("@sid", saleId);
                dCmd.Parameters.AddWithValue("@mid", item.GetIntVal("medicineID"));
                dCmd.Parameters.AddWithValue("@batch", item.GetStringVal("batchNo", "Default"));
                dCmd.Parameters.AddWithValue("@box", item.GetStringVal("RackNo", "Rack-1"));
                dCmd.Parameters.AddWithValue("@qty", item.GetIntVal("quantity"));
                dCmd.Parameters.AddWithValue("@price", item.GetDoubleVal("unitPrice"));
                dCmd.Parameters.AddWithValue("@total", item.GetDoubleVal("totalPrice"));
                dCmd.ExecuteNonQuery();

                // FIFO deduction
                int remaining = item.GetIntVal("quantity");
                var bCmd2 = conn.CreateCommand(); bCmd2.Transaction = tx;
                bCmd2.CommandText = "SELECT StockID,Quantity FROM Stocks WHERE MedicineID=@mid AND Quantity>0 ORDER BY StockID ASC";
                bCmd2.Parameters.AddWithValue("@mid", item.GetIntVal("medicineID"));
                using var bReader = bCmd2.ExecuteReader();
                var stockList = new List<(long id, int qty)>();
                while (bReader.Read()) stockList.Add((bReader.GetInt64(0), (int)bReader.GetInt64(1)));
                bReader.Close();

                foreach (var (sid, sq) in stockList)
                {
                    if (remaining <= 0) break;
                    int deduct = Math.Min(sq, remaining); remaining -= deduct;
                    conn.Execute("UPDATE Stocks SET Quantity=Quantity-@d WHERE StockID=@sid", ("@d", deduct), ("@sid", sid));
                }
            }
        }

        // 4. Ledger entries
        double grandTotal = b.GetDoubleVal("grandTotal");
        string desc2 = $"Sale #{saleId} — {custName}";
        PostLedger(conn, tx, "Cash Account", desc2, debit: grandTotal, credit: 0);
        PostLedger(conn, tx, "Sales Income", desc2, debit: 0, credit: grandTotal);

        tx.Commit();
        return Results.Ok(new { saleId });
    }
    catch (Exception ex)
    {
        tx.Rollback();
        return Results.Problem(ex.Message);
    }
});

// ════════════════════════════════════════════════════════════════════
// PURCHASES
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/purchases", () =>
{
    using var conn = Open();
    return Results.Ok(conn.Query(@"SELECT p.*,COALESCE(s.Name,'Unknown') as SupplierName
        FROM Purchases p LEFT JOIN Suppliers s ON p.SupplierID=s.SupplierID ORDER BY p.PurchaseID DESC LIMIT 100"));
});

app.MapPost("/api/purchases", async (HttpContext ctx) =>
{
    var b = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    using var conn = Open();
    using var tx = conn.BeginTransaction();
    try
    {
        var pCmd = conn.CreateCommand(); pCmd.Transaction = tx;
        pCmd.CommandText = @"INSERT INTO Purchases (SupplierID,InvoiceNo,PurchaseDate,TotalAmount,Tax,Status)
            VALUES (@sid,@inv,@dt,@tot,@tax,@st); SELECT last_insert_rowid();";
        pCmd.Parameters.AddWithValue("@sid", b.GetIntVal("supplierID"));
        pCmd.Parameters.AddWithValue("@inv", b.GetStringVal("invoiceNo"));
        pCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        pCmd.Parameters.AddWithValue("@tot", b.GetDoubleVal("totalAmount"));
        pCmd.Parameters.AddWithValue("@tax", b.GetDoubleVal("tax"));
        pCmd.Parameters.AddWithValue("@st", "Received");
        long purchaseId = (long)(pCmd.ExecuteScalar() ?? 0L);

        if (b.TryGetProperty("items", out var items2))
        {
            foreach (var item in items2.EnumerateArray())
            {
                var dCmd = conn.CreateCommand(); dCmd.Transaction = tx;
                dCmd.CommandText = @"INSERT INTO PurchaseDetails (PurchaseID,MedicineID,BatchNo,ExpiryDate,Quantity,PurchasePrice,TotalPrice)
                    VALUES (@pid,@mid,@batch,@exp,@qty,@pp,@tp)";
                dCmd.Parameters.AddWithValue("@pid", purchaseId);
                dCmd.Parameters.AddWithValue("@mid", item.GetIntVal("medicineID"));
                dCmd.Parameters.AddWithValue("@batch", item.GetStringVal("batchNo","Default"));
                dCmd.Parameters.AddWithValue("@exp", item.GetStringVal("expiryDate",""));
                dCmd.Parameters.AddWithValue("@qty", item.GetIntVal("quantity"));
                dCmd.Parameters.AddWithValue("@pp", item.GetDoubleVal("purchasePrice"));
                dCmd.Parameters.AddWithValue("@tp", item.GetDoubleVal("totalPrice"));
                dCmd.ExecuteNonQuery();

                // Add to stock
                conn.Execute(@"INSERT INTO Stocks (MedicineID,BatchNo,RackNo,ExpiryDate,Quantity,DateAdded)
                    VALUES (@mid,@batch,@box,@exp,@qty,@dt)",
                    ("@mid", item.GetIntVal("medicineID")), ("@batch", item.GetStringVal("batchNo","Default")),
                    ("@box", item.GetStringVal("RackNo","Rack-1")), ("@exp", item.GetStringVal("expiryDate","")),
                    ("@qty", item.GetIntVal("quantity")), ("@dt", DateTime.Now.ToString("yyyy-MM-dd")));
            }
        }

        // Ledger: debit Purchase Expense, credit Cash Account
        string pdesc = $"Purchase #{purchaseId}";
        double ptotal = b.GetDoubleVal("totalAmount");
        PostLedger(conn, tx, "Purchase Expense", pdesc, debit: ptotal, credit: 0);
        PostLedger(conn, tx, "Cash Account", pdesc, debit: 0, credit: ptotal);

        tx.Commit();
        return Results.Ok(new { purchaseId });
    }
    catch (Exception ex)
    {
        tx.Rollback();
        return Results.Problem(ex.Message);
    }
});

// ════════════════════════════════════════════════════════════════════
// CUSTOMERS
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/customers", () =>
{
    using var conn = Open();
    return Results.Ok(conn.Query("SELECT * FROM Customers ORDER BY Name"));
});

app.MapPost("/api/customers", async (HttpContext ctx) =>
{
    var b = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    using var conn = Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "INSERT INTO Customers (Name,Contact,Email,Address,Balance) VALUES (@n,@c,@e,@a,@b); SELECT last_insert_rowid();";
    cmd.Parameters.AddWithValue("@n", b.GetStringVal("name"));
    cmd.Parameters.AddWithValue("@c", b.GetStringVal("contact"));
    cmd.Parameters.AddWithValue("@e", b.GetStringVal("email"));
    cmd.Parameters.AddWithValue("@a", b.GetStringVal("address"));
    cmd.Parameters.AddWithValue("@b", 0);
    long id = (long)(cmd.ExecuteScalar() ?? 0L);
    return Results.Ok(new { id });
});

app.MapPut("/api/customers/{id}", async (int id, HttpContext ctx) =>
{
    var b = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    using var conn = Open();
    conn.Execute("UPDATE Customers SET Name=@n,Contact=@c,Email=@e,Address=@a WHERE CustomerID=@id",
        ("@n", b.GetStringVal("name")), ("@c", b.GetStringVal("contact")),
        ("@e", b.GetStringVal("email")), ("@a", b.GetStringVal("address")), ("@id", id));
    return Results.Ok(new { success = true });
});

app.MapDelete("/api/customers/{id}", (int id) =>
{
    using var conn = Open();
    conn.Execute("DELETE FROM Customers WHERE CustomerID=@id", ("@id", id));
    return Results.Ok(new { success = true });
});

// ════════════════════════════════════════════════════════════════════
// SUPPLIERS
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/suppliers", () =>
{
    using var conn = Open();
    return Results.Ok(conn.Query("SELECT * FROM Suppliers ORDER BY Name"));
});

app.MapPost("/api/suppliers", async (HttpContext ctx) =>
{
    var b = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    using var conn = Open();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "INSERT INTO Suppliers (Name,Contact,Email,Address) VALUES (@n,@c,@e,@a); SELECT last_insert_rowid();";
    cmd.Parameters.AddWithValue("@n", b.GetStringVal("name"));
    cmd.Parameters.AddWithValue("@c", b.GetStringVal("contact"));
    cmd.Parameters.AddWithValue("@e", b.GetStringVal("email"));
    cmd.Parameters.AddWithValue("@a", b.GetStringVal("address"));
    long id = (long)(cmd.ExecuteScalar() ?? 0L);
    return Results.Ok(new { id });
});

app.MapPut("/api/suppliers/{id}", async (int id, HttpContext ctx) =>
{
    var b = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    using var conn = Open();
    conn.Execute("UPDATE Suppliers SET Name=@n,Contact=@c,Email=@e,Address=@a WHERE SupplierID=@id",
        ("@n", b.GetStringVal("name")), ("@c", b.GetStringVal("contact")),
        ("@e", b.GetStringVal("email")), ("@a", b.GetStringVal("address")), ("@id", id));
    return Results.Ok(new { success = true });
});

// ════════════════════════════════════════════════════════════════════
// LEDGERS / ACCOUNTING
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/ledgers", () =>
{
    using var conn = Open();
    return Results.Ok(conn.Query("SELECT * FROM Ledgers ORDER BY Name"));
});

app.MapGet("/api/ledgers/{id}/transactions", (int id) =>
{
    using var conn = Open();
    return Results.Ok(conn.Query($"SELECT * FROM LedgerTransactions WHERE LedgerID={id} ORDER BY TransactionID DESC LIMIT 100"));
});

app.MapPost("/api/ledgers/transaction", async (HttpContext ctx) =>
{
    var b = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
    using var conn = Open();
    using var tx = conn.BeginTransaction();
    PostLedger(conn, tx, b.GetStringVal("ledgerName"), b.GetStringVal("description"),
        b.GetDoubleVal("debit"), b.GetDoubleVal("credit"));
    tx.Commit();
    return Results.Ok(new { success = true });
});

// ════════════════════════════════════════════════════════════════════
// REPORTS
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/reports/sales", (string? filter) =>
{
    string today = DateTime.Now.ToString("yyyy-MM-dd");
    string where = filter switch
    {
        "today" => $"WHERE date(s.SaleDate)='{today}'",
        "month" => $"WHERE strftime('%Y-%m',s.SaleDate)='{today[..7]}'",
        _ => ""
    };
    using var conn = Open();
    return Results.Ok(conn.Query($@"SELECT s.SaleID, COALESCE(c.Name,'Walk-in Customer') as CustomerName,
        s.SaleDate, s.TotalAmount, s.NetPaid, s.Status
        FROM Sales s LEFT JOIN Customers c ON s.CustomerID=c.CustomerID {where}
        ORDER BY s.SaleID DESC LIMIT 500"));
});

app.MapGet("/api/reports/purchases", () =>
{
    using var conn = Open();
    return Results.Ok(conn.Query(@"SELECT p.*,COALESCE(s.Name,'') as SupplierName
        FROM Purchases p LEFT JOIN Suppliers s ON p.SupplierID=s.SupplierID ORDER BY p.PurchaseID DESC LIMIT 500"));
});

// ════════════════════════════════════════════════════════════════════
// HEALTH CHECK
// ════════════════════════════════════════════════════════════════════
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTime.Now }));

app.Run();

// ════════════════════════════════════════════════════════════════════
// HELPERS
// ════════════════════════════════════════════════════════════════════
static string HashPassword(string password)
{
    using var sha = System.Security.Cryptography.SHA256.Create();
    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToHexString(bytes).ToLower();
}

void PostLedger(SqliteConnection conn, SqliteTransaction tx, string ledgerName, string description, double debit, double credit)
{
    var balCmd = conn.CreateCommand(); balCmd.Transaction = tx;
    balCmd.CommandText = "SELECT LedgerID, Balance FROM Ledgers WHERE Name=@n";
    balCmd.Parameters.AddWithValue("@n", ledgerName);
    using var r = balCmd.ExecuteReader();
    if (!r.Read()) return;
    long lid = r.GetInt64(0);
    double balance = r.GetDouble(1) + debit - credit;
    r.Close();

    var tCmd = conn.CreateCommand(); tCmd.Transaction = tx;
    tCmd.CommandText = "INSERT INTO LedgerTransactions (LedgerID,Description,Debit,Credit,Balance) VALUES (@lid,@d,@db,@cr,@bal)";
    tCmd.Parameters.AddWithValue("@lid", lid);
    tCmd.Parameters.AddWithValue("@d", description);
    tCmd.Parameters.AddWithValue("@db", debit);
    tCmd.Parameters.AddWithValue("@cr", credit);
    tCmd.Parameters.AddWithValue("@bal", balance);
    tCmd.ExecuteNonQuery();

    conn.Execute("UPDATE Ledgers SET Balance=@bal WHERE LedgerID=@lid", ("@bal", balance), ("@lid", lid));
}

// ════════════════════════════════════════════════════════════════════
// EXTENSION METHODS (keep code clean)
// ════════════════════════════════════════════════════════════════════
public static class SqliteExtensions
{
    public static T Scalar<T>(this SqliteConnection conn, string sql)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var r = cmd.ExecuteScalar();
        if (r == null || r is DBNull) return default!;
        return (T)Convert.ChangeType(r, typeof(T));
    }

    public static void Execute(this SqliteConnection conn, string sql, params (string, object?)[] parms)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (k, v) in parms) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public static List<Dictionary<string, object?>> Query(this SqliteConnection conn, string sql)
    {
        var result = new List<Dictionary<string, object?>>();
        var cmd = conn.CreateCommand(); cmd.CommandText = sql;
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < r.FieldCount; i++)
                row[r.GetName(i)] = r.IsDBNull(i) ? null : r.GetValue(i);
            result.Add(row);
        }
        return result;
    }

    public static string GetStringVal(this JsonElement e, string key, string def = "") =>
        e.TryGetProperty(key, out var v) ? v.GetString() ?? def : def;
    public static double GetDoubleVal(this JsonElement e, string key, double def = 0) =>
        e.TryGetProperty(key, out var v) && v.TryGetDouble(out var d) ? d : def;
    public static int GetIntVal(this JsonElement e, string key, int def = 0) =>
        e.TryGetProperty(key, out var v) && v.TryGetInt32(out var i) ? i : def;
}
