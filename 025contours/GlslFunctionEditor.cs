using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace _025contours
{
    public partial class GlslFunctionEditor : Form
    {
        public IDictionary<string, string> ImplicitFunctions { get; set; }

        private string lastEditedFunctionName = string.Empty;

        public GlslFunctionEditor()
        {
            InitializeComponent();
            ImplicitFunctions = new Dictionary<string, string>();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            EditFunction();
            string selectedFuncName = (string)functionListBox.SelectedItem;
            if (!string.IsNullOrEmpty(selectedFuncName))
            {
                string sourceCode = ImplicitFunctions[selectedFuncName];
                functionSourceTextBox.Text = (sourceCode != null) ? sourceCode : string.Empty;
            }
            else
            {
                selectedFuncName = string.Empty;
            }
            functionNameTextBox.Text = selectedFuncName;
            lastEditedFunctionName = selectedFuncName;
        }

        private void AddFunction()
        {
            string newFuncName = functionNameTextBox.Text;

            if (!string.IsNullOrEmpty(newFuncName) &&
                !ImplicitFunctions.ContainsKey(newFuncName))
            {
                ImplicitFunctions.Add(newFuncName, functionSourceTextBox.Text);
                lastEditedFunctionName = newFuncName;
                functionListBox.Items.Add(newFuncName);
            }
        }

        private void EditFunction()
        {
            string newFuncName = functionNameTextBox.Text;
            if ((lastEditedFunctionName == newFuncName) &&
                (!string.IsNullOrEmpty(newFuncName) &&
                ImplicitFunctions.ContainsKey(newFuncName)))
            {
                ImplicitFunctions[newFuncName] = functionSourceTextBox.Text;
            }
            else
            {
                if (ImplicitFunctions.ContainsKey(lastEditedFunctionName))
                {
                    ImplicitFunctions.Remove(lastEditedFunctionName);
                    functionListBox.Items.Remove(lastEditedFunctionName);
                }
                AddFunction();
            }
        }

        private void DeleteFunction()
        {
            string selectedFuncName = (string)functionListBox.SelectedItem;
            if (!string.IsNullOrEmpty(selectedFuncName))
            {
                functionNameTextBox.Text = string.Empty;
                functionSourceTextBox.Text = string.Empty;
                ImplicitFunctions.Remove(selectedFuncName);
                functionListBox.Items.Remove(selectedFuncName);
            }
        }

        private void GlslFunctionEditor_Load(object sender, EventArgs e)
        {
            functionListBox.Items.Clear();
            functionNameTextBox.Text = string.Empty;
            functionSourceTextBox.Text = string.Empty;

            foreach (var function in ImplicitFunctions)
            {
                functionListBox.Items.Add(function.Key);
            }
            if (ImplicitFunctions.Count > 0)
            {
                string firstFuncName = ImplicitFunctions.Keys.First();
                functionNameTextBox.Text = firstFuncName;
                functionSourceTextBox.Text = ImplicitFunctions[firstFuncName];
                lastEditedFunctionName = firstFuncName;
            }
            functionSourceTextBox.Focus();
        }

        private void addFunctionButton_Click(object sender, EventArgs e)
        {
            EditFunction();
            functionNameTextBox.Text = string.Empty;
            functionSourceTextBox.Text = string.Empty;
            lastEditedFunctionName = string.Empty;
        }

        private void deleteFunctionButton_Click(object sender, EventArgs e)
        {
            DeleteFunction();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            EditFunction();
        }

        private void GlslFunctionEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            EditFunction();
        }
    }
}
