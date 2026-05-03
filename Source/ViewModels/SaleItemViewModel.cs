using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PharmaBilling.Source.ViewModels
{
    public class SaleItemViewModel : BaseViewModel
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

        private string _RackNo;
        public string RackNo
        {
            get { return _RackNo; }
            set { _RackNo = value; OnPropertyChanged("RackNo"); }
        }

        private double _tp;
        public double TP
        {
            get { return _tp; }
            set { _tp = value; OnPropertyChanged("TP"); }
        }

        private double _retail;
        public double Retail
        {
            get { return _retail; }
            set { _retail = value; OnPropertyChanged("Retail"); }
        }

        private double _quantity;
        public double Quantity
        {
            get { return _quantity; }
            set 
            { 
                _quantity = value; 
                OnPropertyChanged("Quantity"); 
                OnPropertyChanged("TotalPrice");
            }
        }

        private double _unitPrice;
        public double UnitPrice
        {
            get { return _unitPrice; }
            set 
            { 
                _unitPrice = value; 
                OnPropertyChanged("UnitPrice"); 
                OnPropertyChanged("TotalPrice");
            }
        }

        public double TotalPrice
        {
            get { return Quantity * UnitPrice; }
        }
    }
}
