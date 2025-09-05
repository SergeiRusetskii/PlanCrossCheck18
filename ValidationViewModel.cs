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

            // Post-process results: if a validator produced a result per field and
            // every field passed (Info severity), collapse these into a single
            // summary result so the UI only shows the total result.
            var processedResults = results
                .GroupBy(r => r.Category)
                .SelectMany(group =>
                {
                    bool allPass = group.All(r => r.Severity == ValidationSeverity.Info);
                    bool allFieldMessages = group.All(r => r.Message.StartsWith("Field '"));

                    if (allPass && allFieldMessages)
                    {
                        return new[]
                        {
                            new ValidationResult
                            {
                                Category = group.Key,
                                Severity = ValidationSeverity.Info,
                                Message = $"All treatment fields passed {group.Key} checks"
                            }
                        };
                    }

                    return group;
                });

            foreach (var result in processedResults)
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