using System.Collections.Generic;
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

            // Post-process results so that within each category, general and field
            // messages are separated. If all field messages are informational, collapse
            // them into a single summary while leaving general messages untouched.
            var processedResults = results
                .GroupBy(r => r.Category)
                .SelectMany(group =>
                {
                    var fieldResults = group.Where(r => r.Message.StartsWith("Field '")).ToList();
                    var generalResults = group.Where(r => !r.Message.StartsWith("Field '")).ToList();

                    var output = new List<ValidationResult>();

                    foreach (var r in generalResults)
                    {
                        r.SubCategory = "General";
                        output.Add(r);
                    }

                    if (fieldResults.Any())
                    {
                        bool allPass = fieldResults.All(r => r.Severity == ValidationSeverity.Info);
                        if (allPass)
                        {
                            output.Add(new ValidationResult
                            {
                                Category = group.Key,
                                SubCategory = "Fields",
                                Severity = ValidationSeverity.Info,
                                Message = $"All treatment fields passed {group.Key} checks"
                            });
                        }
                        else
                        {
                            foreach (var r in fieldResults)
                            {
                                r.SubCategory = "Fields";
                                output.Add(r);
                            }
                        }
                    }

                    return output;
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
        public string SubCategory { get; set; }
        public ValidationSeverity Severity { get; set; }

        // Optional computed property for backward compatibility
        public bool IsValid => Severity != ValidationSeverity.Error;
    }
}
