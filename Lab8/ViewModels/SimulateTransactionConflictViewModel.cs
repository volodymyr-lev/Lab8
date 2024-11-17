using System;
using System.Data;
using System.Windows.Input;
using Lab8.Services;

namespace Lab8.ViewModels
{
    public class SimulateTransactionConflictViewModel : BaseViewModel
    {
        private readonly DataService _dataService;

        public SimulateTransactionConflictViewModel()
        {
            _dataService = new DataService();
            SimulateConflictCommand = new RelayCommand(ExecuteSimulateConflict);
        }

        public ICommand SimulateConflictCommand { get; }

        private int _parcelId;
        public int ParcelId
        {
            get => _parcelId;
            set => SetProperty(ref _parcelId, value);
        }

        private void ExecuteSimulateConflict()
        {
            try
            {
                _dataService.BeginTransaction(IsolationLevel.ReadCommitted);
                _dataService.SimulateTransactionConflict(ParcelId);
                ResultMessage = "Conflict simulated successfully!";
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
