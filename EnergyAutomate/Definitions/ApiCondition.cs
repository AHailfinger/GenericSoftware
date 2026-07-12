namespace EnergyAutomate.Definitions
{
    public class ApiCondition
    {
        public ApiCondition(Func<Task>? callback, Func<Task<bool>>? validation)
        {
            Callback = callback;
            Validation = validation;
        }

        public Func<Task>? Callback { get; set; }
        public Func<Task<bool>>? Validation { get; set; }
    }
}
