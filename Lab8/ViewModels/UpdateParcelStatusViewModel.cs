using System;
using System.Data;
using System.Windows.Input;
using Lab8.Services;

namespace Lab8.ViewModels
{
    public class UpdateParcelStatusViewModel : BaseViewModel
    {
        private readonly DataService _dataService;

        public UpdateParcelStatusViewModel()
        {
            _dataService = new DataService();
            UpdateParcelStatusCommand = new RelayCommand(ExecuteUpdateParcelStatus);
        }

        public ICommand UpdateParcelStatusCommand { get; }

        private int _parcelId;
        public int ParcelId
        {
            get => _parcelId;
            set => SetProperty(ref _parcelId, value);
        }

        private string _newStatus;
        public string NewStatus
        {
            get => _newStatus;
            set => SetProperty(ref _newStatus, value);
        }

        private void ExecuteUpdateParcelStatus()
        {
            try
            {
                _dataService.BeginTransaction(IsolationLevel.ReadCommitted);
                _dataService.UpdateParcelStatus(ParcelId, NewStatus);
                ResultMessage = "Status updated successfully!";
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
