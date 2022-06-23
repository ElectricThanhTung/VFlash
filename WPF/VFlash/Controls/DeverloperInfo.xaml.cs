using System.Windows.Controls;
using System.Windows.Media;

namespace VFlash {
    public partial class DeverloperInfo : UserControl {
        public DeverloperInfo() {
            InitializeComponent();
        }

        public string DevName {
            get {
                return NameTextBox.Text;
            }
            set {
                NameTextBox.Text = value;
            }
        }

        public string Email {
            get {
                return EmailTextBox.Text;
            }
            set {
                EmailTextBox.Text = value;
            }
        }

        public ImageSource ImageSource {
            get {
                return AvatarImage.ImageSource;
            }
            set {
                AvatarImage.ImageSource = value;
            }
        }
    }
}
