using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace InventoryView
{
    public class InventoryViewForm : Form
    {
        private List<TreeNode> searchMatches = new List<TreeNode>();
        private TreeNode currentMatch;
        private IContainer components;
        private TreeView tv;
        private TextBox txtSearch;
        private CheckedListBox chkCharacters;
        private Label lblSearch;
        private Label lblFound;
        private Button btnSearch;
        private Button btnExpand;
        private Button btnCollapse;
        private Button btnWiki;
        private Button btnReset;
        private Button btnFindNext;
        private Button btnFindPrev;
        private Button btnScan;
        private Button btnReload;
        private ToolStripMenuItem copyTapToolStripMenuItem;
        private ToolStripMenuItem exportBranchToFileToolStripMenuItem;
        private ContextMenuStrip contextMenuStrip1;
        private Button btnExport;
        private ToolTip toolTip = new ToolTip();
        private ListBox lb1;
        private ContextMenuStrip listBox_Menu;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem wikiToolStripMenuItem;
        private ToolStripMenuItem copyAllToolStripMenuItem;
        private ToolStripMenuItem copySelectedToolStripMenuItem;
        private bool clickSearch;
        private ToolStripMenuItem wikiLookupToolStripMenuItem;
        private ListBox listBox = new ListBox();

        public InventoryViewForm() => InitializeComponent();

        private void InventoryViewForm_Load(object sender, EventArgs e) => BindData();

        private void BindData()
        {
            chkCharacters.Items.Clear();
            tv.Nodes.Clear();
            List<string> list = InventoryView.characterData.Select<CharacterData, string>((Func<CharacterData, string>)(tbl => tbl.name)).Distinct<string>().ToList<string>();
            list.Sort();
            foreach (string str in list)
            {
                string character = str;
                chkCharacters.Items.Add((object)character, true);
                TreeNode treeNode1 = tv.Nodes.Add(character);
                foreach (CharacterData characterData in InventoryView.characterData.Where<CharacterData>((Func<CharacterData, bool>)(tbl => tbl.name == character)))
                {
                    TreeNode treeNode2 = treeNode1.Nodes.Add(characterData.source);
                    treeNode2.ToolTipText = treeNode2.FullPath;
                    PopulateTree(treeNode2, characterData.items);
                }
            }
        }

        private void PopulateTree(TreeNode treeNode, List<ItemData> itemList)
        {
            foreach (ItemData itemData in itemList)
            {
                TreeNode treeNode1 = treeNode.Nodes.Add(itemData.tap);
                treeNode1.ToolTipText = treeNode1.FullPath;
                if (itemData.items.Count<ItemData>() > 0)
                    PopulateTree(treeNode1, itemData.items);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSearch.Text))
            {
                searchMatches.Clear();
                lb1.Items.Clear();
                currentMatch = (TreeNode)null;
                tv.CollapseAll();
                SearchTree(tv.Nodes);
                clickSearch = true;
            }
            btnFindNext.Visible = btnFindPrev.Visible = btnReset.Visible = (uint)searchMatches.Count<TreeNode>() > 0U;
            lblFound.Text = "Found: " + searchMatches.Count.ToString();
            if (searchMatches.Count<TreeNode>() == 0)
                return;
            btnFindNext.PerformClick();
        }

        private bool SearchTree(TreeNodeCollection nodes)
        {
            bool flag = false;
            string nodeList;
            foreach (TreeNode node in nodes)
            {
                node.BackColor = Color.White;
                if (SearchTree(node.Nodes))
                {
                    node.Expand();
                    flag = true;
                }
                bool result = node.Text.IndexOf(txtSearch.Text, StringComparison.OrdinalIgnoreCase) >= 0;
                if (result == true)
                {
                    node.Expand();
                    node.BackColor = Color.Yellow;
                    flag = true;

                    searchMatches.Add(node);

                    nodeList = node.ToString();
                    if (nodeList.StartsWith("TreeNode: "))
                        nodeList = nodeList.Remove(0, 10);

                    nodeList = Regex.Replace(nodeList, @"\(\d+\)\s", "");
                    if (nodeList[nodeList.Length - 1] == '.')
                        nodeList = nodeList.TrimEnd('.');
                    lb1.Items.Add(nodeList);
                }
            }
            return flag;
        }

        private bool resetTree(TreeNodeCollection nodes)
        {
            bool flag = false;
            foreach (TreeNode node in nodes)
            {
                node.BackColor = Color.White;
                if (resetTree(node.Nodes))
                {
                    flag = true;
                }
                bool result = node.Text.IndexOf(txtSearch.Text, StringComparison.OrdinalIgnoreCase) >= 0;
                if (result == true)
                {
                    node.BackColor = Color.White;
                    flag = true;
                }
            }
            return flag;
        }

        private void btnExpand_Click(object sender, EventArgs e) => tv.ExpandAll();

        private void btnCollapse_Click(object sender, EventArgs e) => tv.CollapseAll();

        private void btnWiki_Click(object sender, EventArgs e)
        {
            if (tv.SelectedNode == null)
            {
                int num = (int)MessageBox.Show("Select an item to lookup.");
            }
            else
                Process.Start(string.Format("https://drservice.info/wiki.ashx?tap={0}", (object)Regex.Replace(tv.SelectedNode.Text, @"\(\d+\)\s|\s\(closed\)", "")));
        }

        private void Wiki_Click(object sender, EventArgs e)
        {
            if (tv.SelectedNode == null)
            {
                int num = (int)MessageBox.Show("Select an item to lookup.");
            }
            else
                Process.Start(string.Format("https://drservice.info/wiki.ashx?tap={0}", (object)Regex.Replace(tv.SelectedNode.Text, @"\(\d+\)\s|\s\(closed\)", "")));
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            btnSearch.PerformClick();
            btnFindNext.Visible = btnFindPrev.Visible = btnReset.Visible = clickSearch = false;
            tv.CollapseAll();
            lb1.Items.Clear();
            lblFound.Text = "Found: 0";
            resetTree(tv.Nodes);
            searchMatches.Clear();
            currentMatch = (TreeNode)null;
            txtSearch.Text = "";
        }

        private void btnFindPrev_Click(object sender, EventArgs e)
        {
            if (currentMatch == null)
            {
                currentMatch = searchMatches.Last<TreeNode>();
            }
            else
            {
                currentMatch.BackColor = Color.Yellow;
                int index = searchMatches.IndexOf(currentMatch) - 1;
                if (index == -1)
                    index = searchMatches.Count<TreeNode>() - 1;
                currentMatch = searchMatches[index];
            }
            currentMatch.EnsureVisible();
            currentMatch.BackColor = Color.LightBlue;
        }

        private void btnFindNext_Click(object sender, EventArgs e)
        {
            if (currentMatch == null)
            {
                currentMatch = searchMatches.First<TreeNode>();
            }
            else
            {
                currentMatch.BackColor = Color.Yellow;
                int index = searchMatches.IndexOf(currentMatch) + 1;
                if (index == searchMatches.Count<TreeNode>())
                    index = 0;
                currentMatch = searchMatches[index];
            }
            currentMatch.EnsureVisible();
            currentMatch.BackColor = Color.LightBlue;
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            InventoryView._host.SendText("/InventoryView scan");
            Close();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            InventoryView.LoadSettings();
            InventoryView._host.EchoText("Inventory reloaded.");
            BindData();
        }

        private void copyTapToolStripMenuItem_Click(object sender, EventArgs e) => Clipboard.SetText(Regex.Replace(tv.SelectedNode.Text, @"\(\d+\)\s", ""));

        private void exportBranchToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> branchText = new List<string>();
            branchText.Add(Regex.Replace(tv.SelectedNode.Text, @"\(\d+\)\s", ""));
            copyBranchText(tv.SelectedNode.Nodes, branchText, 1);
            Clipboard.SetText(string.Join("\r\n", branchText.ToArray()));
        }

        private void copyBranchText(TreeNodeCollection nodes, List<string> branchText, int level)
        {
            foreach (TreeNode node in nodes)
            {
                branchText.Add(new string('\t', level) + Regex.Replace(node.Text, @"\(\d+\)\s", ""));
                copyBranchText(node.Nodes, branchText, level + 1);
            }
        }

        private void listBox_Copy_Click(object sender, EventArgs e)
        {
            if (lb1.SelectedItem == null)
            {
                int num = (int)MessageBox.Show("Select an item to copy.");
            }
            else
            {
                StringBuilder txt = new StringBuilder();
                foreach (object row in lb1.SelectedItems)
                {
                    txt.Append(row.ToString());
                    txt.AppendLine();
                }
                txt.Remove(txt.Length - 1, 1);
                Clipboard.SetData(System.Windows.Forms.DataFormats.Text, txt.ToString());
            }
        }

        public void listBox_Copy_All_Click(Object sender, EventArgs e)
        {
            if (clickSearch == false)
            {
                int num = (int)MessageBox.Show("Must search first to copy all.");
            }
            else
            {
                StringBuilder buffer = new StringBuilder();

                for (int i = 0; i < lb1.Items.Count; i++)
                {
                    buffer.Append(lb1.Items[i].ToString());
                    buffer.Append("\n");
                }
                Clipboard.SetText(buffer.ToString());
            }
        }

        public void listBox_Copy_All_Selected_Click(Object sender, EventArgs e)
        {
            if (lb1.SelectedItem == null)
            {
                int num = (int)MessageBox.Show("Select items to copy.");
            }
            else
            {
                StringBuilder buffer = new StringBuilder();

                for (int i = 0; i < lb1.SelectedItems.Count; i++)
                {
                    buffer.Append(lb1.SelectedItems[i].ToString());
                    buffer.Append("\n");
                }
                Clipboard.SetText(buffer.ToString());
            }
        }

        private void listbox_Wiki_Click(object sender, EventArgs e)
        {
            if (lb1.SelectedItem == null)
            {
                int num = (int)MessageBox.Show("Select an item to lookup.");
            }
            else
                Process.Start(string.Format("https://drservice.info/wiki.ashx?tap={0}", (object)lb1.SelectedItem.ToString().Replace(" (closed)", "")));
        }

        private void lb1_MouseDown(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control || (Control.ModifierKeys == Keys.Control || Control.ModifierKeys == Keys.ShiftKey || e.Button == MouseButtons.Left))
            {
                lb1.SelectionMode = SelectionMode.MultiExtended;
            }
            else if (e.Button == MouseButtons.Left)
            {
                lb1.SelectionMode = SelectionMode.One;
            }
        }

        private void tv_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            Point point = new Point(e.X, e.Y);
            TreeNode nodeAt = tv.GetNodeAt(point);
            if (nodeAt == null)
                return;
            tv.SelectedNode = nodeAt;
            contextMenuStrip1.Show((Control)tv, point);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV file|*.csv";
            saveFileDialog.Title = "Save the CSV file";
            int num1 = (int)saveFileDialog.ShowDialog();
            if (!(saveFileDialog.FileName != ""))
                return;
            using (StreamWriter text = File.CreateText(saveFileDialog.FileName))
            {
                List<InventoryViewForm.ExportData> list = new List<InventoryViewForm.ExportData>();
                exportBranch(tv.Nodes, list, 1);
                text.WriteLine("Character,Tap,Path");
                foreach (InventoryViewForm.ExportData exportData in list)
                {
                    if (exportData.Path.Count >= 1)
                    {
                        if (exportData.Path.Count == 3)
                        {
                            if (((IEnumerable<string>)new string[2]
                            {
                "Vault",
                "Home"
                            }).Contains<string>(exportData.Path[1]))
                                continue;
                        }
                        text.WriteLine(string.Format("{0},{1},{2}", (object)CleanCSV(exportData.Character), (object)CleanCSV(exportData.Tap), (object)CleanCSV(string.Join("\\", (IEnumerable<string>)exportData.Path))));
                    }
                }
            }
            int num2 = (int)MessageBox.Show("Export Complete.");
        }

        private string CleanCSV(string data)
        {
            if (!data.Contains(","))
                return data;
            return !data.Contains("\"") ? string.Format("\"{0}\"", (object)data) : string.Format("\"{0}\"", (object)data.Replace("\"", "\"\""));
        }

        private void exportBranch(
          TreeNodeCollection nodes,
          List<InventoryViewForm.ExportData> list,
          int level)
        {
            foreach (TreeNode node in nodes)
            {
                InventoryViewForm.ExportData exportData = new InventoryViewForm.ExportData()
                {
                    Tap = node.Text
                };
                TreeNode treeNode = node;
                while (treeNode.Parent != null)
                {
                    treeNode = treeNode.Parent;
                    if (treeNode.Parent != null)
                        exportData.Path.Insert(0, treeNode.Text);
                }
                exportData.Character = treeNode.Text;
                list.Add(exportData);
                exportBranch(node.Nodes, list, level + 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tv = new System.Windows.Forms.TreeView();
            txtSearch = new System.Windows.Forms.TextBox();
            chkCharacters = new System.Windows.Forms.CheckedListBox();
            lblSearch = new System.Windows.Forms.Label();
            lblFound = new System.Windows.Forms.Label();
            btnSearch = new System.Windows.Forms.Button();
            btnExpand = new System.Windows.Forms.Button();
            btnCollapse = new System.Windows.Forms.Button();
            btnWiki = new System.Windows.Forms.Button();
            btnReset = new System.Windows.Forms.Button();
            btnFindNext = new System.Windows.Forms.Button();
            btnFindPrev = new System.Windows.Forms.Button();
            btnScan = new System.Windows.Forms.Button();
            btnReload = new System.Windows.Forms.Button();
            btnExport = new System.Windows.Forms.Button();
            copyTapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exportBranchToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            wikiLookupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            lb1 = new System.Windows.Forms.ListBox();
            listBox_Menu = new System.Windows.Forms.ContextMenuStrip(components);
            copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            wikiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copyAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copySelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            contextMenuStrip1.SuspendLayout();
            listBox_Menu.SuspendLayout();
            SuspendLayout();
            // 
            // tv
            // 
            tv.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            tv.Location = new System.Drawing.Point(5, 55);
            tv.Name = "tv";
            tv.ShowNodeToolTips = true;
            tv.Size = new System.Drawing.Size(612, 407);
            tv.TabIndex = 10;
            tv.MouseUp += new System.Windows.Forms.MouseEventHandler(tv_MouseUp);
            // 
            // txtSearch
            // 
            txtSearch.Location = new System.Drawing.Point(62, 18);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(262, 20);
            txtSearch.TabIndex = 1;
            // 
            // chkCharacters
            // 
            chkCharacters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            chkCharacters.FormattingEnabled = true;
            chkCharacters.Location = new System.Drawing.Point(974, 41);
            chkCharacters.Name = "chkCharacters";
            chkCharacters.Size = new System.Drawing.Size(136, 19);
            chkCharacters.TabIndex = 9;
            chkCharacters.Visible = false;
            // 
            // lblSearch
            // 
            lblSearch.AutoSize = true;
            lblSearch.Location = new System.Drawing.Point(12, 21);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new System.Drawing.Size(44, 13);
            lblSearch.TabIndex = 0;
            lblSearch.Text = "Search:";
            // 
            // lblFound
            // 
            lblFound.AutoSize = true;
            lblFound.Location = new System.Drawing.Point(353, 42);
            lblFound.Name = "lblFound";
            lblFound.Size = new System.Drawing.Size(49, 13);
            lblFound.TabIndex = 0;
            lblFound.Text = "Found: 0";
            // 
            // btnSearch
            // 
            btnSearch.Location = new System.Drawing.Point(353, 16);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new System.Drawing.Size(75, 23);
            btnSearch.TabIndex = 2;
            btnSearch.Text = "Search";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += new System.EventHandler(btnSearch_Click);
            // 
            // btnExpand
            // 
            btnExpand.Location = new System.Drawing.Point(596, 5);
            btnExpand.Name = "btnExpand";
            btnExpand.Size = new System.Drawing.Size(75, 23);
            btnExpand.TabIndex = 6;
            btnExpand.Text = "Expand All";
            btnExpand.UseVisualStyleBackColor = true;
            btnExpand.Click += new System.EventHandler(btnExpand_Click);
            // 
            // btnCollapse
            // 
            btnCollapse.Location = new System.Drawing.Point(596, 27);
            btnCollapse.Name = "btnCollapse";
            btnCollapse.Size = new System.Drawing.Size(75, 23);
            btnCollapse.TabIndex = 7;
            btnCollapse.Text = "Collapse All";
            btnCollapse.UseVisualStyleBackColor = true;
            btnCollapse.Click += new System.EventHandler(btnCollapse_Click);
            // 
            // btnWiki
            // 
            btnWiki.Location = new System.Drawing.Point(677, 16);
            btnWiki.Name = "btnWiki";
            btnWiki.Size = new System.Drawing.Size(75, 23);
            btnWiki.TabIndex = 8;
            btnWiki.Text = "Wiki Lookup";
            btnWiki.UseVisualStyleBackColor = true;
            btnWiki.Click += new System.EventHandler(btnWiki_Click);
            // 
            // btnReset
            // 
            btnReset.Location = new System.Drawing.Point(515, 16);
            btnReset.Name = "btnReset";
            btnReset.Size = new System.Drawing.Size(75, 23);
            btnReset.TabIndex = 5;
            btnReset.Text = "Reset";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Visible = false;
            btnReset.Click += new System.EventHandler(btnReset_Click);
            // 
            // btnFindNext
            // 
            btnFindNext.Location = new System.Drawing.Point(434, 27);
            btnFindNext.Name = "btnFindNext";
            btnFindNext.Size = new System.Drawing.Size(75, 23);
            btnFindNext.TabIndex = 4;
            btnFindNext.Text = "Find Next";
            btnFindNext.UseVisualStyleBackColor = true;
            btnFindNext.Visible = false;
            btnFindNext.Click += new System.EventHandler(btnFindNext_Click);
            // 
            // btnFindPrev
            // 
            btnFindPrev.Location = new System.Drawing.Point(434, 5);
            btnFindPrev.Name = "btnFindPrev";
            btnFindPrev.Size = new System.Drawing.Size(75, 23);
            btnFindPrev.TabIndex = 3;
            btnFindPrev.Text = "Find Prev";
            btnFindPrev.UseVisualStyleBackColor = true;
            btnFindPrev.Visible = false;
            btnFindPrev.Click += new System.EventHandler(btnFindPrev_Click);
            // 
            // btnScan
            // 
            btnScan.Location = new System.Drawing.Point(838, 16);
            btnScan.Name = "btnScan";
            btnScan.Size = new System.Drawing.Size(97, 23);
            btnScan.TabIndex = 11;
            btnScan.Text = "Scan Inventory";
            btnScan.UseVisualStyleBackColor = true;
            btnScan.Click += new System.EventHandler(btnScan_Click);
            // 
            // btnReload
            // 
            btnReload.Location = new System.Drawing.Point(941, 16);
            btnReload.Name = "btnReload";
            btnReload.Size = new System.Drawing.Size(97, 23);
            btnReload.TabIndex = 12;
            btnReload.Text = "Reload File";
            btnReload.UseVisualStyleBackColor = true;
            btnReload.Click += new System.EventHandler(btnReload_Click);
            // 
            // btnExport
            // 
            btnExport.Location = new System.Drawing.Point(758, 16);
            btnExport.Name = "btnExport";
            btnExport.Size = new System.Drawing.Size(75, 23);
            btnExport.TabIndex = 13;
            btnExport.Text = "Export";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += new System.EventHandler(btnExport_Click);
            // 
            // copyTapToolStripMenuItem
            // 
            copyTapToolStripMenuItem.Name = "copyTapToolStripMenuItem";
            copyTapToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            copyTapToolStripMenuItem.Text = "Copy Text";
            copyTapToolStripMenuItem.Click += new System.EventHandler(copyTapToolStripMenuItem_Click);
            // 
            // exportBranchToFileToolStripMenuItem
            // 
            exportBranchToFileToolStripMenuItem.Name = "exportBranchToFileToolStripMenuItem";
            exportBranchToFileToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            exportBranchToFileToolStripMenuItem.Text = "Copy Branch";
            exportBranchToFileToolStripMenuItem.Click += new System.EventHandler(exportBranchToFileToolStripMenuItem_Click);
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            copyTapToolStripMenuItem,
            exportBranchToFileToolStripMenuItem,
            wikiLookupToolStripMenuItem});
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(143, 70);
            // 
            // wikiLookupToolStripMenuItem
            // 
            wikiLookupToolStripMenuItem.Name = "wikiLookupToolStripMenuItem";
            wikiLookupToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            wikiLookupToolStripMenuItem.Text = "Wiki Lookup";
            wikiLookupToolStripMenuItem.Click += new System.EventHandler(Wiki_Click);
            // 
            // lb1
            // 
            lb1.AllowDrop = true;
            lb1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            lb1.ContextMenuStrip = listBox_Menu;
            lb1.FormattingEnabled = true;
            lb1.Location = new System.Drawing.Point(624, 55);
            lb1.Name = "lb1";
            lb1.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            lb1.Size = new System.Drawing.Size(492, 407);
            lb1.TabIndex = 10;
            lb1.MouseDown += new System.Windows.Forms.MouseEventHandler(lb1_MouseDown);
            // 
            // listBox_Menu
            // 
            listBox_Menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            copyToolStripMenuItem,
            wikiToolStripMenuItem,
            copyAllToolStripMenuItem,
            copySelectedToolStripMenuItem});
            listBox_Menu.Name = "listBox_Menu";
            listBox_Menu.Size = new System.Drawing.Size(167, 92);
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            copyToolStripMenuItem.Text = "Copy Selected";
            copyToolStripMenuItem.Click += new System.EventHandler(listBox_Copy_Click);
            // 
            // wikiToolStripMenuItem
            // 
            wikiToolStripMenuItem.Name = "wikiToolStripMenuItem";
            wikiToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            wikiToolStripMenuItem.Text = "Wiki Selected";
            wikiToolStripMenuItem.Click += new System.EventHandler(listbox_Wiki_Click);
            // 
            // copyAllToolStripMenuItem
            // 
            copyAllToolStripMenuItem.Name = "copyAllToolStripMenuItem";
            copyAllToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            copyAllToolStripMenuItem.Text = "Copy All";
            copyAllToolStripMenuItem.Click += new System.EventHandler(listBox_Copy_All_Click);
            // 
            // copySelectedToolStripMenuItem
            // 
            copySelectedToolStripMenuItem.Name = "copySelectedToolStripMenuItem";
            copySelectedToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            copySelectedToolStripMenuItem.Text = "Copy All Selected";
            copySelectedToolStripMenuItem.Click += new System.EventHandler(listBox_Copy_All_Selected_Click);
            // 
            // InventoryViewForm
            // 
            AcceptButton = btnSearch;
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(1122, 478);
            Controls.Add(lb1);
            Controls.Add(btnExport);
            Controls.Add(btnReload);
            Controls.Add(btnScan);
            Controls.Add(btnFindNext);
            Controls.Add(btnFindPrev);
            Controls.Add(btnReset);
            Controls.Add(btnWiki);
            Controls.Add(btnCollapse);
            Controls.Add(btnExpand);
            Controls.Add(btnSearch);
            Controls.Add(lblSearch);
            Controls.Add(lblFound);
            Controls.Add(chkCharacters);
            Controls.Add(txtSearch);
            Controls.Add(tv);
            Name = "InventoryViewForm";
            Text = "Inventory View";
            Load += new System.EventHandler(InventoryViewForm_Load);
            contextMenuStrip1.ResumeLayout(false);
            listBox_Menu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        public class ExportData
        {
            public string Character { get; set; }

            public string Tap { get; set; }

            public List<string> Path { get; set; } = new List<string>();
        }
    }
}
