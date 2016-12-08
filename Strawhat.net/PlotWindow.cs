using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Strawhat.net
{
    public partial class PlotWindow : Form
    {
        BindingList<double> x = new BindingList<double>();

        public PlotWindow()
        {
            InitializeComponent();
        }

        private void addData(double data)
        {

        }
    }
}
