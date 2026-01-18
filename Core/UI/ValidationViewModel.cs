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
                .SelectMany<IGrouping<string, ValidationResult>, ValidationResult>(group =>
                {
                    var groupList = group.ToList();
                    bool allPass = groupList.All(r => r.Severity == ValidationSeverity.Info);
                    bool allFieldResults = groupList.All(r => r.IsFieldResult);
                    bool hasMultipleResults = groupList.Count > 1;

                    if (allPass && allFieldResults && hasMultipleResults)
                    {
                        // Use custom AllPassSummary if provided, otherwise use generic message
                        string summaryMessage = groupList.FirstOrDefault()?.AllPassSummary
                            ?? $"All treatment fields passed {group.Key} checks";

                        return new[]
                        {
                            new ValidationResult
                            {
                                Category = group.Key,
                                Severity = ValidationSeverity.Info,
                                Message = summaryMessage
                            }
                        };
                    }

                    return groupList;
                });

            // Sort results by category order
            var sortedResults = processedResults
                .OrderBy(r => GetCategoryOrder(r.Category))
                .ThenBy(r => r.Category);

            foreach (var result in sortedResults)
            {
                ValidationResults.Add(result);
            }
        }

        /// <summary>
        /// Defines the display order for validation result categories.
        /// Lower numbers appear first in the UI.
        /// </summary>
        private int GetCategoryOrder(string category)
        {
            // Define category order
            if (category.StartsWith("Course")) return 10;
            if (category.StartsWith("CT.Curve")) return 20;
            if (category.StartsWith("Plan.Info")) return 30;
            if (category.StartsWith("PlanningStructures")) return 40;
            if (category.StartsWith("Fixation")) return 50;
            if (category.StartsWith("Collision")) return 60;
            if (category.StartsWith("CT.UserOrigin")) return 70;
            if (category.StartsWith("Fields")) return 80;
            if (category.StartsWith("Dose")) return 90;
            if (category.StartsWith("Optimization")) return 100;

            // Unknown categories appear at the end
            return 999;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
