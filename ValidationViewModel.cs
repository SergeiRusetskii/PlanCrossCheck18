using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace PlanCrossCheck
{
    public class ValidationViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ValidationResult> ValidationResults { get; } = new ObservableCollection<ValidationResult>();

        public ValidationViewModel(ScriptContext context)
        {
            // Create and use the root validator
            var rootValidator = new RootValidator();
            var results = rootValidator.Validate(context);

            // Add all results to the observable collection
            foreach (var result in results)
            {
                ValidationResults.Add(result);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ValidationResult
    {
        public string Message { get; set; }
        public string Category { get; set; }
        public ValidationSeverity Severity { get; set; }

        // Optional computed property for backward compatibility
        public bool IsValid => Severity != ValidationSeverity.Error;
    }
}