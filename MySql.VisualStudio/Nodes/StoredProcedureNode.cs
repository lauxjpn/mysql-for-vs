using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using Microsoft.VisualStudio.Data;
using System.Windows.Forms;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;
using System.Data;
using MySql.Data.VisualStudio.Editors;
using MySql.Data.VisualStudio.Properties; 

namespace MySql.Data.VisualStudio
{
	class StoredProcedureNode : DocumentNode
	{
		private string sql_mode;
		private bool isFunction;
        private TextBufferEditor editor;

		public StoredProcedureNode(DataViewHierarchyAccessor hierarchyAccessor, int id) : 
			base(hierarchyAccessor, id)
		{
            NodeId = "StoredProcedure";
            NameIndex = 3;
            editor = new TextBufferEditor();
        }

        #region Properties

        public override string SchemaCollection
        {
            get { return "procedures"; }
        }

        public override bool Dirty
        {
            get { return (editor as TextBufferEditor).Dirty; }
            protected set { (editor as TextBufferEditor).Dirty = value; }
        }

        private bool IsFunction
		{
			get { return NodeId.ToLowerInvariant() == "storedfunction"; }
        }

        #endregion

        public static void CreateNew(DataViewHierarchyAccessor HierarchyAccessor)
        {
            StoredProcedureNode node = new StoredProcedureNode(HierarchyAccessor, 0);
            node.Edit();
        }

        public override object GetEditor()
        {
            return editor;
        }

		public override string GetDropSQL()
		{
			return String.Format("DROP {0} `{1}`.`{2}`", 
				IsFunction ? "FUNCTION" : "PROCEDURE", Database, Name);
		}

        public override string GetSaveSql()
        {
            return editor.Text;
        }

        protected override void  Load()
        {
            if (IsNew)
                editor.Text = "CREATE PROCEDURE " + Name + "() BEGIN END";
            else
            {
                try
                {
                    DataTable dt = GetDataTable(String.Format("SHOW CREATE {0} `{1}`.`{2}`",
                            IsFunction ? "FUNCTION" : "PROCEDURE", Database, Name));

                    sql_mode = dt.Rows[0][1] as string;
                    string sql = dt.Rows[0][2] as string;
                    editor.Text = ChangeSqlTypeTo(sql, "ALTER");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to load object with error: " + ex.Message);
                }
            }
		}

        /// <summary>
        /// We override save here so we can change the sql from create to alter on
        /// first save
        /// </summary>
        /// <returns></returns>
        protected override bool Save()
        {
            // if we are a new sproc then we don't need to do any of our
            // alter "magic" (see below)
            if (IsNew) return SaveAsNew();

            // since MySQL doesn't support altering the body of a proc we have
            // to do some "magic"

            // first we need to check the syntax of our changes.  THis will throw
            // an exception if the syntax is bad
            try
            {
                CheckSyntax();

                string createSql = ChangeSqlTypeTo(editor.Text.Trim(), "CREATE");
                ExecuteSQL(String.Format("DROP PROCEDURE IF EXISTS `{0}`.`{1}`", Database, Name));
                ExecuteSQL(createSql);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "MySQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool SaveAsNew()
        {
            bool saveOk = base.Save();
            editor.Text = ChangeSqlTypeTo(editor.Text.Trim(), "ALTER");
            return saveOk;
        }

        private void CheckSyntax()
        {
            string sql = editor.Text.Trim();
            sql = ChangeSqlTypeTo(sql, "CREATE");
            try
            {
                ExecuteSQL(sql);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("syntax"))
                    throw;
            }
        }

        private string ChangeSqlTypeTo(string sql, string type)
        {
            int index = sql.IndexOf(' ');
            string startingCommand = sql.Substring(0, index).ToUpperInvariant();
            if (startingCommand != "CREATE" && startingCommand != "ALTER")
                throw new Exception(Resources.UnableToExecuteProcScript);
            return type + sql.Substring(index);
        }

        /// <summary>
        /// Parse the name of the procedure out of the sql block.  We have to do this
        /// because we need the name of the proc for our node path but the user has
        /// probably change the name from the default
        /// </summary>
        /// <param name="sql">The sql block to parse</param>
        private void ParseName(string sql)
        {
            string lowerSql = sql.ToLowerInvariant();
            int pos = lowerSql.IndexOf("procedure") + 9;
            int end = lowerSql.IndexOf("(", pos);
            string procName = sql.Substring(pos, end - pos).Trim();
            Name = procName;
        }
    }
}