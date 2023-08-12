
namespace GyroPrompt.Basic_Objects.Variables
{
    public class BooleanVariable : LocalVariable
    {
        private bool value_;
        public bool bool_val
        {
            get { return value_; }
            set
            {
                value_ = value;
                Value = value.ToString();
            }
        }
        public void Toggle()
        {
            if (bool_val == true) { bool_val = false; } else if (bool_val == false) { bool_val = true; }
        }
    }
}
