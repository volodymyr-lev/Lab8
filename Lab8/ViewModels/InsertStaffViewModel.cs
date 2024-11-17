using System;
using System.Data;
using System.Windows.Input;
using Lab8.Services;

namespace Lab8.ViewModels
{
    public class InsertStaffViewModel : BaseViewModel
    {
        private readonly DataService _dataService;

        public InsertStaffViewModel()
        {
            _dataService = new DataService();
            InsertStaffCommand = new RelayCommand(ExecuteInsertStaff);
        }

        public ICommand InsertStaffCommand { get; }

        private string _staffName;
        public string StaffName
        {
            get => _staffName;
            set => SetProperty(ref _staffName, value);
        }

        private string _staffPosition;
        public string StaffPosition
        {
            get => _staffPosition;
            set => SetProperty(ref _staffPosition, value);
        }

        private int _postOfficeId;
        public int PostOfficeId
        {
            get => _postOfficeId;
            set => SetProperty(ref _postOfficeId, value);
        }

        private string _phoneNumber;
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        private void ExecuteInsertStaff()
        {
            try
            {
                _dataService.BeginTransaction(IsolationLevel.ReadCommitted);
                _dataService.InsertStaff(StaffName, StaffPosition, PostOfficeId, PhoneNumber, StartDate);
                ResultMessage = "Data inserted successfully!";
                _dataService.CommitTransaction();
            }
            catch (Exception ex)
            {
                _dataService.RollbackTransaction();
                ResultMessage = $"Error: {ex.Message}";
            }
        }
    }
}
