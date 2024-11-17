using System;
using System.Data;
using System.Windows.Input;
using Lab8.Services;

namespace Lab8.ViewModels
{
    public class ResolveConflictWithHigherIsolationViewModel : BaseViewModel
    {
        private readonly DataService _dataService;

        public ResolveConflictWithHigherIsolationViewModel()
        {
            _dataService = new DataService();
            ResolveConflictCommand = new RelayCommand(ExecuteResolveConflict);
        }

        public ICommand ResolveConflictCommand { get; }

        private int _parcelId;
        public int ParcelId
        {
            get => _parcelId;
            set => SetProperty(ref _parcelId, value);
        }

        private void ExecuteResolveConflict()
        {
            try
            {
                _dataService.BeginTransaction(IsolationLevel.Serializable);
                _dataService.ResolveConflictWithHigherIsolation(ParcelId);
                ResultMessage = "Conflict resolved successfully!";
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
