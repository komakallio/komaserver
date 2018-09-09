using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace ASCOM.Komakallio
{
    [ComVisible(false)]					// Form not registered for COM!
    public partial class SetupDialogForm : Form
    {
        public SetupDialogForm()
        {
            InitializeComponent();
            // Initialise current values of user settings from the ASCOM Profile
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            // Place any validation constraint checks here
            // Update the state variables with results from the dialogue
            SafetyMonitor.ServerAddress = (string)serverAddressTextBox.Text;
            SafetyMonitor.Filters = detailsListView.Items
                .Cast<ListViewItem>()
                .Select(x => new Filter()
                {
                    Name = x.Text,
                    Checked = x.Checked,
                })
                .ToList();
        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("http://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        private void InitUI()
        {
            serverAddressTextBox.Text = SafetyMonitor.ServerAddress;
            foreach (var filter in SafetyMonitor.Filters)
            {
                detailsListView.Items.Add(filter.Name);
                detailsListView.Items[detailsListView.Items.Count - 1].Checked = filter.Checked;
            }
        }

        private void refreshDetailsButton_Click(object sender, EventArgs e)
        {
            try
            {
                detailsListView.Clear();
                refreshDetailsButton.Enabled = false;
                var status = new SafetyServer(serverAddressTextBox.Text).Status;

                foreach (var detail in status.Details.Keys)
                {
                    detailsListView.Items.Add(detail);
                    detailsListView.Items[detailsListView.Items.Count - 1].Checked = true;
                }
            }
            catch (Exception)
            {
                detailsListView.Clear();
                MessageBox.Show("Could not connect to server!", "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                refreshDetailsButton.Enabled = true;
            }
        }
    }
}