﻿using System.Text;
using DatabaseSchemaReader.DataSchema;

namespace DatabaseSchemaReader.CodeGen
{
    class OverrideWriter
    {
        private readonly ClassBuilder _cb;
        private readonly DatabaseTable _table;

        public OverrideWriter(ClassBuilder classBuilder, DatabaseTable table)
        {
            _cb = classBuilder;
            _table = table;
            NetName = table.NetName;
        }

        public string NetName { get; set; }

        /// <summary>
        /// Adds the overrides (composite key version)
        /// </summary>
        public void AddOverrides()
        {
            //if there is no pk, these won't work
            if (_table.PrimaryKey == null) return;

            _cb.AppendLine("#region overrides");

            AddToString();
            AddGetHashCode();
            AddEquals();

            _cb.AppendLine("#endregion");
        }

        private void AddEquals()
        {
            using (_cb.BeginNest("public override bool Equals(object obj)"))
            {
                _cb.AppendLine("var x = obj as " + NetName + ";");
                _cb.AppendLine("if (x == null) return false;");

                foreach (var column in _table.Columns)
                {
                    if (!column.IsPrimaryKey) continue;

                    var primaryKeyName = column.NetName;
                    var datatype = column.DataType;
                    if (column.IsForeignKey)
                    {
                        _cb.AppendLine("if (" + primaryKeyName + " == null && x." + primaryKeyName + " == null) return ReferenceEquals(this, x);");
                    }
                    else if (datatype.IsNumeric)
                    {
                        _cb.AppendLine("if (" + primaryKeyName + " == 0 && x." + primaryKeyName + " == 0) return ReferenceEquals(this, x);");
                    }
                    else if (datatype.IsString)
                    {
                        _cb.AppendLine("if (string.IsNullOrEmpty(" + primaryKeyName + ") && string.IsNullOrEmpty(" + primaryKeyName + ")) return object.ReferenceEquals(this, x);");
                    }
                }
                var sb = new StringBuilder();
                sb.Append("return ");
                var i = 0;
                foreach (var column in _table.Columns)
                {
                    if (!column.IsPrimaryKey) continue;
                    if (i != 0) sb.Append(" && ");
                    i++;
                    var primaryKeyName = column.NetName;
                    sb.Append("(" + primaryKeyName + " == x." + primaryKeyName + ")");
                }
                sb.AppendLine(";");
                _cb.AppendLine(sb.ToString());
            }


        }

        private void AddGetHashCode()
        {
            using (_cb.BeginNest("public override int GetHashCode()"))
            {

                //first check if any key is transient
                foreach (var column in _table.Columns)
                {
                    if (!column.IsPrimaryKey) continue;
                    var primaryKeyName = column.NetName;
                    var datatype = column.DataType;
                    if (column.IsForeignKey)
                    {
                        _cb.AppendLine("if (" + primaryKeyName + " == null) return base.GetHashCode(); //transient instance");
                    }
                    else if (datatype.IsNumeric)
                    {
                        _cb.AppendLine("if (" + primaryKeyName + " == 0) return base.GetHashCode(); //transient instance");
                    }
                    else if (datatype.IsString)
                    {
                        _cb.AppendLine("if (string.IsNullOrEmpty(" + primaryKeyName +
                                      ")) return base.GetHashCode(); //transient instance");
                    }
                }
                //persistent object, just get the keys (we don't have to worry about nulls here)
                var sb = new StringBuilder();
                var i = 0;
                sb.Append("return ");
                foreach (var column in _table.Columns)
                {
                    if (!column.IsPrimaryKey) continue;
                    if (i != 0) sb.Append(" ^ "); //XOR hashcodes together
                    i++;
                    var primaryKeyName = column.NetName;
                    var datatype = column.DataType;
                    sb.Append(primaryKeyName);
                    if (datatype.IsInt && !column.IsForeignKey) continue;
                    sb.Append(".GetHashCode()");

                }
                sb.AppendLine(";");
                _cb.AppendLine(sb.ToString());
            }
        }

        private void AddToString()
        {
            using (_cb.BeginNest("public override string ToString()"))
            {
                var sb = new StringBuilder();

                sb.Append("return \"[");
                var i = 0;
                foreach (var column in _table.Columns)
                {
                    if (!column.IsPrimaryKey) continue;

                    if (i != 0) sb.Append(" + \" [");
                    i++;
                    sb.Append(column.NetName + "] = \" + " + column.NetName);
                }
                sb.AppendLine(";");

                _cb.AppendLine(sb.ToString());
            }
        }
    }
}
