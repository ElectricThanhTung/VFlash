using System;
using System.Windows.Controls;

namespace VFlash {
    public partial class EcuTab : UserControl {
        private static event EventHandler TabSelectedIndexChanged;
        private static int tabSelectedIndex = 0;

        public EcuTab() {
            InitializeComponent();
            MainTab.SelectedIndex = TabSelectedIndex;
            TabSelectedIndexChanged += SyncTabSelectedIndex;
            MainTab.SelectionChanged += (o, e) => {
                TabSelectedIndex = ((TabControl)o).SelectedIndex;
            };
        }

        private static int TabSelectedIndex {
            get {
                return tabSelectedIndex;
            }
            set {
                if(tabSelectedIndex != value) {
                    tabSelectedIndex = value;
                    TabSelectedIndexChanged?.Invoke(null, null);
                }
            }
        }

        private void SyncTabSelectedIndex(object sender, EventArgs e) {
            MainTab.SelectedIndex = TabSelectedIndex;
        }

        public void UnlinkTabIndex() {
            TabSelectedIndexChanged -= SyncTabSelectedIndex;
        }
    }
}
