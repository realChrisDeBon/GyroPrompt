
namespace GyroPrompt.Basic_Objects.Variables
{
    public class StringVariable : LocalVariable
    {
        private string str_value_ { get; set; }
        
        public string str_value
        {
            get { return str_value_; }
            set
            {
                str_value_ = value;
                Value = str_value_;
            }
        }
    }
}
