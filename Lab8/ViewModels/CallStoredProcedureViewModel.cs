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

        private int _parcelId;
        public int ParcelId
        {
            get => _parcelId;
            set => SetProperty(ref _parcelId, value);
        }
        private double _weight;
        public double Weight
        {
            get=> _weight;
            set=> SetProperty(ref _weight, value);
        }

        private void ExecuteCallStoredProcedure()
        {
            try
            {
                _dataService.BeginTransaction(IsolationLevel.ReadCommitted);
                _dataService.CallStoredProcedure(ParcelId, Weight);
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
