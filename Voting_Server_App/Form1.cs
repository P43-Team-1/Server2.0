using System.Text;
using Voting_Server_App.Context;

namespace Voting_Server_App
{
    public partial class Form1 : Form
    {
        VoteContext context;
        DataBaseScript script = new DataBaseScript();

        public Form1()
        {
            InitializeComponent();
            context = new VoteContext();
            script.OnLogMessage += LogMessage;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
         
        }

        private bool CheckDataBase()
        {
            return context.Database.CanConnect();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button3.Enabled = false;
            LogMessage("Checking database connection...");
            await Task.Run(() =>
            {
                if (!CheckDataBase())
                {
                    LogMessage("Database connection failed.");
                    DialogResult result = MessageBox.Show("Database connection failed. Create new database?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (result == DialogResult.Yes)
                    {
                        script.CreateDatabase();
                        LogMessage("Database created successfully.");
                    }
                    button1.BeginInvoke(() => { button1.Enabled = true; });
                    button3.BeginInvoke(() => { button3.Enabled = true; });
                    return;
                }
                button2.BeginInvoke(() => { button2.Enabled = true; });
                Task.Run(() => { script.StartServer(); });
                LogMessage("Server is now running and ready to accept connections.");
                label1.BeginInvoke(() =>
                {
                    label1.Text = "Status: Online";
                });
            });
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to create a new database? This will overwrite any existing data.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                await Task.Run(() => { script.CreateDatabase(); });
                LogMessage("Database created successfully.");
            }
        }

        private void LogMessage(string message)
        {
            StringBuilder sb = new StringBuilder(textBox1.Text);
            sb.AppendLine($"[{DateTime.Now.ToShortTimeString()}] {message}");
            textBox1.BeginInvoke(() => { textBox1.Text = sb.ToString(); });
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;

            LogMessage("Stopping database server...");

            await Task.Run(() => { script.StopServer(); });

            button1.Enabled = true;
            button3.Enabled = true;
            LogMessage("Database server stopped.");
            label1.BeginInvoke(() =>
            {
                label1.Text = "Status: Offline";
            });
        }
    }
}
