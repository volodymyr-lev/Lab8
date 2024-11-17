using System;
using System.Data;
using System.Windows.Input;
using Lab8.Services;

namespace Lab8.ViewModels
{
    public class CallStoredProcedureViewModel : BaseViewModel
    {
        private readonly DataService _dataService;

        public CallStoredProcedureViewModel()
        {
            _dataService = new DataService();
            CallStoredProcedureCommand = new RelayCommand(ExecuteCallStoredProcedure);
        }

        public ICommand CallStoredProcedureCommand { get; }

        private int _shipmentId;
        public int ShipmentId
        {
            get => _shipmentId;
            set => SetProperty(ref _shipmentId, value);
        }

        private void ExecuteCallStoredProcedure()
        {
            try
            {
                _dataService.BeginTransaction(IsolationLevel.ReadCommitted);
                _dataService.CallStoredProcedure(ShipmentId);
                ResultMessage = "Procedure called successfully!";
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
