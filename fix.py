import os
import re

dirs=['Source/Data', 'Source/ViewModels', 'Source/Views']
for d in dirs:
  for root, _, files in os.walk(d):
    for f in files:
      if f.endswith('.cs'):
        path = os.path.join(root, f)
        with open(path, 'r', encoding='utf-8') as file:
          content = file.read()
        
        orig = content
        
        # 1. Fix auto property initializers
        content = re.sub(r'public static string MasterServerIP\s*\{\s*get;\s*set;\s*\}\s*=\s*".*?";', r'public static string MasterServerIP { get; set; }', content)
        content = re.sub(r'public static string SyncSecret\s*\{\s*get;\s*set;\s*\}\s*=\s*".*?";', r'public static string SyncSecret { get; set; }', content)
        content = re.sub(r'public static bool IsServer\s*\{\s*get;\s*set;\s*\}\s*=\s*true;', r'public static bool IsServer { get; set; }', content)
        content = re.sub(r'public string Role\s*\{\s*get;\s*set;\s*\}\s*=\s*".*?";', r'public string Role { get; set; }', content)
        content = re.sub(r'public static int CurrentUserId\s*\{\s*get;\s*set;\s*\}\s*=\s*\d+;', r'public static int CurrentUserId { get; set; }', content)
        content = re.sub(r'public static string Username\s*\{\s*get;\s*set;\s*\}\s*=\s*".*?";', r'public static string Username { get; set; }', content)
        content = re.sub(r'public static string Role\s*\{\s*get;\s*set;\s*\}\s*=\s*".*?";', r'public static string Role { get; set; }', content)

        # 2. Fix Expression Bodied Properties (=>) 
        content = re.sub(r'public string Username\s*=>\s*(.*?);', r'public string Username { get { return \1; } }', content)
        content = re.sub(r'public string Role\s*=>\s*(.*?);', r'public string Role { get { return \1; } }', content)
        content = re.sub(r'public bool CanBuy\s*=>\s*(.*?);', r'public bool CanBuy { get { return \1; } }', content)
        content = re.sub(r'public bool CanReturn\s*=>\s*(.*?);', r'public bool CanReturn { get { return \1; } }', content)

        # 3. Fix Expression Bodied Methods (=>)
        content = re.sub(r'public void AddPurchase\(\)\s*=>\s*(.*?);', r'public void AddPurchase() { \1; }', content)
        content = re.sub(r'public void SavePurchase\(\)\s*=>\s*(.*?);', r'public void SavePurchase() { \1; }', content)
        content = re.sub(r'public bool Refresh\(\)\s*=>\s*(.*?);', r'public bool Refresh() { return \1; }', content)
        
        # 4. Fix String Interpolation $""
        content = re.sub(r'\$\"Welcome, \{AppSession\.Username\}\"', r'string.Format("Welcome, {0}", AppSession.Username)', content)
        content = re.sub(r'\$\"Report Date: \{DateTime\.Now:dd MMM yyyy\}\"', r'string.Format("Report Date: {0:dd MMM yyyy}", DateTime.Now)', content)
        # Using string format simply by replacing specific known strings
        content = content.replace('$"No data found for {type}. Please adjust your filters."', 'string.Format("No data found for {0}. Please adjust your filters.", type)')
        content = content.replace('$"Backup saved to: {folderPath}"', 'string.Format("Backup saved to: {0}", folderPath)')
        content = content.replace('$"Are you sure you want to delete Invoice #{item.InvoiceNo}?\\nThis action cannot be undone."', 'string.Format("Are you sure you want to delete Invoice #{0}?\\nThis action cannot be undone.", item.InvoiceNo)')
        content = content.replace('$"Are you sure you want to update role to {role}?"', 'string.Format("Are you sure you want to update role to {0}?", role)')
        content = content.replace('$"Role updated to {role} successfully."', 'string.Format("Role updated to {0} successfully.", role)')
        content = content.replace('$"Are you sure you want to delete {item.Username}?"', 'string.Format("Are you sure you want to delete {0}?", item.Username)')

        if content != orig:
          with open(path, 'w', encoding='utf-8') as file:
            file.write(content)
          print('Fixed', path)
