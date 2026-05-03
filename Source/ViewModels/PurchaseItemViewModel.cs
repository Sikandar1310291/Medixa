using System;

namespace PharmaBilling.Source.ViewModels
{
    public class PurchaseItemViewModel : BaseViewModel
    {
        private int _medicineID;
        public int MedicineID
        {
            get { return _medicineID; }
            set { _medicineID = value; OnPropertyChanged("MedicineID"); }
        }

        private string _medicineName;
        public string MedicineName
        {
            get { return _medicineName; }
            set { _medicineName = value; OnPropertyChanged("MedicineName"); }
        }

        private string _batchNo;
        public string BatchNo
        {
            get { return _batchNo; }
            set { _batchNo = value; OnPropertyChanged("BatchNo"); }
        }

        private string _rackNo;
        public string RackNo
        {
            get { return _rackNo; }
            set { _rackNo = value; OnPropertyChanged("RackNo"); }
        }

        private string _expiryDate;
        public string ExpiryDate
        {
            get { return _expiryDate; }
            set { _expiryDate = value; OnPropertyChanged("ExpiryDate"); }
        }

        // ── PACKS & PACK SIZE (Auto-calculate Quantity) ───────────────────────
        private string _packsStr = "1";
        public string PacksStr
        {
            get { return _packsStr; }
            set
            {
                _packsStr = value;
                OnPropertyChanged("PacksStr");
                OnPropertyChanged("Packs");
                RecalculateQuantity();
            }
        }
        /// <summary>Number of packs/boxes being purchased.</summary>
        public double Packs
        {
            get { return double.TryParse(_packsStr, out double v) ? (v < 0 ? 0 : v) : 0; }
            set { PacksStr = value.ToString(); }
        }

        private string _packSizeStr = "1";
        public string PackSizeStr
        {
            get { return _packSizeStr; }
            set
            {
                _packSizeStr = value;
                OnPropertyChanged("PackSizeStr");
                OnPropertyChanged("PackSize");
                RecalculateQuantity();
                RecalculateRetailQty();
            }
        }
        /// <summary>Units per pack (e.g. 10 tablets per strip, 30 strips per box).</summary>
        public double PackSize
        {
            get { return double.TryParse(_packSizeStr, out double v) ? (v < 1 ? 1 : v) : 1; }
            set { PackSizeStr = value.ToString(); }
        }

        private string _looseQtyStr = "0";
        public string LooseQtyStr
        {
            get { return _looseQtyStr; }
            set
            {
                _looseQtyStr = value;
                OnPropertyChanged("LooseQtyStr");
                OnPropertyChanged("LooseQty");
                RecalculateQuantity();
            }
        }
        /// <summary>Loose units being purchased alongside full packs.</summary>
        public double LooseQty
        {
            get { return double.TryParse(_looseQtyStr, out double v) ? (v < 0 ? 0 : v) : 0; }
            set { LooseQtyStr = value.ToString(); }
        }

        private string _quantityStr = "1";
        public string QuantityStr
        {
            get { return _quantityStr; }
            set
            {
                _quantityStr = value;
                OnPropertyChanged("QuantityStr");
                OnPropertyChanged("Quantity");
                CalculateTotal();
            }
        }
        /// <summary>Total units = (Packs x PackSize) + LooseQty. Auto-updated.</summary>
        public double Quantity
        {
            get { return double.TryParse(_quantityStr, out double v) ? v : 0; }
            set { QuantityStr = value.ToString(); }
        }

        private void RecalculateQuantity()
        {
            double q = (Packs * PackSize) + LooseQty;
            _quantityStr = q.ToString();
            OnPropertyChanged("QuantityStr");
            OnPropertyChanged("Quantity");
            RecalculateRetailQty();
            CalculateTotal();
        }

        private void RecalculateRetailQty()
        {
            if (PackSize > 0)
            {
                RetailQtyStr = (Retail / PackSize).ToString("0.##");
            }
        }

        // ── PRICING ───────────────────────────────────────────────────────────
        private string _tpStr = "0";
        public string TPStr
        {
            get { return _tpStr; }
            set
            {
                _tpStr = value;
                OnPropertyChanged("TPStr");
                OnPropertyChanged("TP");
                CalculateTotal();
            }
        }
        public double TP
        {
            get { return double.TryParse(_tpStr, out double v) ? v : 0; }
            set { TPStr = value.ToString(); }
        }

        private string _retailStr = "0";
        public string RetailStr
        {
            get { return _retailStr; }
            set 
            { 
                _retailStr = value; 
                OnPropertyChanged("RetailStr"); 
                OnPropertyChanged("Retail");
                RecalculateRetailQty();
            }
        }
        public double Retail
        {
            get { return double.TryParse(_retailStr, out double v) ? v : 0; }
            set { RetailStr = value.ToString(); }
        }

        private string _retailQtyStr = "0";
        public string RetailQtyStr
        {
            get { return _retailQtyStr; }
            set { 
                _retailQtyStr = value; 
                OnPropertyChanged("RetailQtyStr"); 
                OnPropertyChanged("RetailQty");
            }
        }
        public double RetailQty
        {
            get { return double.TryParse(_retailQtyStr, out double v) ? v : 0; }
            set { RetailQtyStr = value.ToString(); }
        }

        private double _totalPrice;
        public double TotalPrice
        {
            get { return _totalPrice; }
            set { _totalPrice = value; OnPropertyChanged("TotalPrice"); }
        }

        private void CalculateTotal()
        {
            if (Packs > 0)
            {
                // Normal Purchase Mode (TP is per Pack)
                double unitTP = PackSize > 0 ? TP / PackSize : 0;
                TotalPrice = (TP * Packs) + (unitTP * LooseQty);
            }
            else
            {
                // Loose Purchase Mode (TP is per Unit, Quantity is the raw unit count)
                TotalPrice = TP * Quantity;
            }
        }
    }
}
