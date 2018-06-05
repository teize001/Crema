﻿//Released under the MIT License.
//
//Copyright (c) 2018 Ntreev Soft co., Ltd.
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//documentation files (the "Software"), to deal in the Software without restriction, including without limitation the 
//rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
//persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
//Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Ntreev.Crema.Data;
using Ntreev.Crema.Data.Xml;
using Ntreev.Crema.Data.Xml.Schema;
using Ntreev.Crema.ServiceModel;
using Ntreev.Crema.Services.Properties;
using Ntreev.Library;
using Ntreev.Library.IO;
using Ntreev.Library.Linq;
using Ntreev.Library.ObjectModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

#pragma warning disable 0612

namespace Ntreev.Crema.Services.Data
{
    class TableCollection : TableCollectionBase<Table, TableCategory, TableCollection, TableCategoryCollection, TableContext>,
        ITableCollection
    {
        private ItemsCreatedEventHandler<ITable> tablesCreated;
        private ItemsRenamedEventHandler<ITable> tablesRenamed;
        private ItemsMovedEventHandler<ITable> tablesMoved;
        private ItemsDeletedEventHandler<ITable> tablesDeleted;
        private ItemsEventHandler<ITable> tablesChanged;
        private ItemsEventHandler<ITable> tablesStateChanged;

        public TableCollection()
        {

        }

        public Table AddNew(Authentication authentication, string name, string categoryPath)
        {
            if (NameValidator.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidName_Format, name), nameof(name));
            return this.BaseAddNew(name, categoryPath, authentication);
        }

        public Table AddNew(Authentication authentication, CremaTemplate template)
        {
            var dataSet = template.TargetTable.DataSet.Copy();
            var dataTable = dataSet.Tables[template.TableName, template.CategoryPath];
            var categoryPath = dataTable.CategoryPath;

            this.ValidateAddNew(dataTable.TableName, categoryPath, authentication);
            this.Sign(authentication);
            this.InvokeTableCreate(authentication, dataTable.TableName, categoryPath, dataTable, null);
            var table = this.AddNew(authentication, dataTable.TableName, categoryPath);
            table.Initialize(dataTable.TableInfo);
            this.InvokeTablesCreatedEvent(authentication, new Table[] { table }, dataSet);
            return table;
        }

        public Table Inherit(Authentication authentication, Table table, string newTableName, string categoryPath, bool copyContent)
        {
            this.ValidateInherit(authentication, table, newTableName, categoryPath, copyContent);
            this.Sign(authentication);
            var dataTable = table.ReadData(authentication);
            var itemName = new ItemName(categoryPath, newTableName);
            var newDataTable = dataTable.Inherit(itemName, copyContent);
            if (copyContent == false)
                newDataTable.Clear();
            newDataTable.CategoryPath = categoryPath;

            this.InvokeTableCreate(authentication, newTableName, categoryPath, newDataTable, table);
            var newTable = this.AddNew(authentication, newTableName, categoryPath);
            newTable.TemplatedParent = table;
            newTable.Initialize(newDataTable.TableInfo);
            foreach (var item in newDataTable.Childs)
            {
                var childTable = this.AddNew(authentication, item.Name, categoryPath);
                childTable.TemplatedParent = table.Childs[item.TableName];
                childTable.Initialize(item.TableInfo);
            }
            var items = EnumerableUtility.Friends(newTable, newTable.Childs).ToArray();
            this.InvokeTablesCreatedEvent(authentication, items, dataTable.DataSet);
            return newTable;
        }

        public Table Copy(Authentication authentication, Table table, string newTableName, string categoryPath, bool copyContent)
        {
            this.ValidateCopy(authentication, table, newTableName, categoryPath, copyContent);
            this.Sign(authentication);
            var dataTable = table.ReadData(authentication);
            var itemName = new ItemName(categoryPath, newTableName);
            var newDataTable = dataTable.Copy(itemName, copyContent);
            if (copyContent == false)
                newDataTable.Clear();
            newDataTable.CategoryPath = categoryPath;

            this.InvokeTableCreate(authentication, newTableName, categoryPath, newDataTable, table);
            var newTable = this.AddNew(authentication, newTableName, categoryPath);
            newTable.Initialize(newDataTable.TableInfo);
            foreach (var item in newDataTable.Childs)
            {
                var childTable = this.AddNew(authentication, item.Name, categoryPath);
                childTable.Initialize(item.TableInfo);
            }
            var items = EnumerableUtility.Friends(newTable, newTable.Childs).ToArray();
            this.InvokeTablesCreatedEvent(authentication, items, dataTable.DataSet);
            return newTable;
        }

        public object GetService(System.Type serviceType)
        {
            return this.DataBase.GetService(serviceType);
        }

        public void InvokeTableCreate(Authentication authentication, string tableName, string categoryPath, CremaDataTable dataTable, Table sourceTable)
        {
            this.CremaHost.DebugMethod(authentication, this, nameof(InvokeTableCreate), tableName, categoryPath, sourceTable);
            this.ValidateCreate(authentication, categoryPath);

            var itemPath = this.Context.GenerateTablePath(categoryPath, tableName);
            var comment = EventMessageBuilder.CreateTable(authentication, new string[] { tableName });
            try
            {
                if (sourceTable != null)
                {
                    var sourceItemPath = this.Context.GenerateTablePath(categoryPath, sourceTable.Name);
                    var props = new PropertyCollection
                    {
                        { nameof(Table.TemplatedParent), dataTable.CategoryPath + dataTable.Name }
                    };
                    var items1 = this.Serializer.GetPath(sourceItemPath, dataTable.GetType(), props);
                    var items2 = this.Serializer.GetPath(itemPath, dataTable.GetType(), props);
                    for (var i = 0; i < items1.Length; i++)
                    {
                        this.Repository.Copy(items1[i], items2[i]);
                    }
                    this.Serializer.Serialize(itemPath, dataTable, props);
                }
                else
                {
                    var itemPaths = this.Serializer.Serialize(itemPath, dataTable, null);
                    foreach (var item in itemPaths)
                    {
                        this.Repository.Add(item);
                    }
                }
                this.Context.InvokeTableItemCreate(authentication, categoryPath + tableName);

                this.Repository.Commit(authentication, comment);
                this.CremaHost.Info(comment);
            }
            catch (Exception e)
            {
                this.CremaHost.Error(e);
                this.Repository.Revert();
                throw e;
            }
        }

        public void InvokeTableRename(Authentication authentication, Table table, string newName, CremaDataSet dataSet)
        {
            this.CremaHost.DebugMethod(authentication, this, nameof(InvokeTableRename), table, newName);
            var dataTables = new DataTableCollection(dataSet, this.DataBase);
            var dataTable = dataSet.Tables[table.Name, table.Category.Path];
            var comment = EventMessageBuilder.RenameTable(authentication, new string[] { newName }, new string[] { table.Name });
            try
            {
                dataTable.TableName = newName;
                dataTables.Modify(this.Serializer);
                dataTables.Move(this.Repository, this.Serializer);
                this.Context.InvokeTableItemRename(authentication, table, newName);
                this.Repository.Commit(authentication, comment);
                this.CremaHost.Info(comment);
            }
            catch (Exception e)
            {
                this.CremaHost.Error(e);
                this.Repository.Revert();
                throw e;
            }
        }

        public void InvokeTableMove(Authentication authentication, Table table, string newCategoryPath, CremaDataSet dataSet)
        {
            this.CremaHost.DebugMethod(authentication, this, nameof(InvokeTableMove), table, newCategoryPath);
            var dataTables = new DataTableCollection(dataSet, this.DataBase);
            var dataTable = dataSet.Tables[table.Name, table.Category.Path];
            var comment = EventMessageBuilder.MoveTable(authentication, new string[] { table.Name }, new string[] { table.Category.Path }, new string[] { newCategoryPath });
            try
            {
                dataTable.CategoryPath = newCategoryPath;
                dataTables.Modify(this.Serializer);
                dataTables.Move(this.Repository, this.Serializer);
                this.Context.InvokeTableItemMove(authentication, table, newCategoryPath);
                this.Repository.Commit(authentication, comment);
                this.CremaHost.Info(comment);
            }
            catch (Exception e)
            {
                this.CremaHost.Error(e);
                this.Repository.Revert();
                throw e;
            }
        }

        public void InvokeTableDelete(Authentication authentication, Table table, CremaDataSet dataSet)
        {
            this.CremaHost.DebugMethod(authentication, this, nameof(InvokeTableDelete), table);
            var comment = EventMessageBuilder.DeleteTable(authentication, new string[] { table.Name });
            try
            {
                if (dataSet != null)
                {
                    var dataTables = dataSet != null ? new DataTableCollection(dataSet, this.DataBase) : null;
                    var dataTable = dataSet.Tables[table.Name, table.Category.Path];
                    var parentTable = dataTable.Parent;
                    if (parentTable != null)
                        parentTable.Childs.Remove(dataTable);
                    else
                        dataSet.Tables.Remove(dataTable);
                    dataTables.Modify(this.Serializer);
                }
                else
                {
                    if (table.TemplatedParent == null)
                        this.Repository.Delete(table.SchemaPath);

                    this.Repository.Delete(table.XmlPath);
                }
                this.Context.InvokeTableItemDelete(authentication, table);
                this.Repository.Commit(authentication, comment);
                this.CremaHost.Info(comment);
            }
            catch (Exception e)
            {
                this.CremaHost.Error(e);
                this.Repository.Revert();
                throw;
            }
        }

        public void InvokeTableEndContentEdit(Authentication authentication, Table table, CremaDataSet dataSet)
        {
            this.CremaHost.DebugMethod(authentication, this, nameof(InvokeTableEndContentEdit), table);
            var dataTables = new DataTableCollection(dataSet, this.DataBase);
            var comment = EventMessageBuilder.ChangeTableContent(authentication, new string[] { table.Name });
            try
            {
                dataTables.Modify(this.Serializer);
                this.Context.InvokeTableItemChange(authentication, table);
                this.Repository.Commit(authentication, comment);
                this.CremaHost.Info(comment);
            }
            catch (Exception e)
            {
                this.CremaHost.Error(e);
                this.Repository.Revert();
                throw e;
            }
        }

        public void InvokeTableEndTemplateEdit(Authentication authentication, Table table, CremaTemplate template)
        {
            this.CremaHost.DebugMethod(authentication, this, nameof(InvokeTableEndTemplateEdit), table);
            var dataTable = template.TargetTable;
            var dataSet = dataTable.DataSet;
            var dataTables = new DataTableCollection(dataSet, this.DataBase);
            var comment = EventMessageBuilder.ChangeTableTemplate(authentication, new string[] { table.Name });
            try
            {
                dataTables.Modify(this.Serializer);
                this.Context.InvokeTableItemChange(authentication, table);
                this.Repository.Commit(authentication, comment);
                this.CremaHost.Info(comment);
            }
            catch (Exception e)
            {
                this.CremaHost.Error(e);
                this.Repository.Revert();
                throw e;
            }
        }

        public void InvokeTableSetTags(Authentication authentication, Table table, TagInfo tags, CremaDataSet dataSet)
        {
            this.CremaHost.DebugMethod(authentication, this, nameof(InvokeTableSetTags), table, tags);
            var dataTables = new DataTableCollection(dataSet, this.DataBase);
            var dataTable = dataSet.Tables[table.Name, table.Category.Path];
            var comment = EventMessageBuilder.ChangeTableTemplate(authentication, new string[] { table.Name });
            try
            {
                dataTable.Tags = tags;
                dataTables.Modify(this.Serializer);
                dataTables.Move(this.Repository, this.Serializer);
                this.Context.InvokeTableItemChange(authentication, table);
                this.Repository.Commit(authentication, comment);
                this.CremaHost.Info(comment);
            }
            catch (Exception e)
            {
                this.CremaHost.Error(e);
                this.Repository.Revert();
                throw e;
            }
        }

        public void InvokeTableSetComment(Authentication authentication, Table table, string comment, CremaDataSet dataSet)
        {
            this.CremaHost.DebugMethod(authentication, this, nameof(InvokeTableSetComment), table, comment);
            var dataTables = new DataTableCollection(dataSet, this.DataBase);
            var dataTable = dataSet.Tables[table.Name, table.Category.Path];
            var commentMessage = EventMessageBuilder.ChangeTableTemplate(authentication, new string[] { table.Name });
            try
            {
                dataTable.Comment = comment;
                dataTables.Modify(this.Serializer);
                dataTables.Move(this.Repository, this.Serializer);
                this.Context.InvokeTableItemChange(authentication, table);
                this.Repository.Commit(authentication, commentMessage);
                this.CremaHost.Info(commentMessage);
            }
            catch (Exception e)
            {
                this.CremaHost.Error(e);
                this.Repository.Revert();
                throw e;
            }
        }

        public void InvokeChildTableCreate(Authentication authentication, Table table, string childName, CremaDataSet dataSet)
        {
            this.CremaHost.DebugMethod(authentication, this, nameof(InvokeChildTableCreate), table);
            var dataTables = new DataTableCollection(dataSet, this.DataBase);
            var comment = EventMessageBuilder.CreateTable(authentication, new string[] { childName });
            try
            {
                dataTables.Modify(this.Serializer);
                this.Repository.Commit(authentication, comment);
                this.CremaHost.Info(comment);
            }
            catch (Exception e)
            {
                this.CremaHost.Error(e);
                this.Repository.Revert();
                throw e;
            }
        }

        public void InvokeTablesCreatedEvent(Authentication authentication, Table[] tables, CremaDataSet dataSet)
        {
            var args = tables.Select(item => (object)item.TableInfo).ToArray();
            var eventLog = EventLogBuilder.BuildMany(authentication, this, nameof(InvokeTablesCreatedEvent), tables);
            this.CremaHost.Debug(eventLog);
            this.OnTablesCreated(new ItemsCreatedEventArgs<ITable>(authentication, tables, args, dataSet));
            this.Context.InvokeItemsCreatedEvent(authentication, tables, args, dataSet);
        }

        public void InvokeTablesRenamedEvent(Authentication authentication, Table[] tables, string[] oldNames, string[] oldPaths, CremaDataSet dataSet)
        {
            var eventLog = EventLogBuilder.BuildMany(authentication, this, nameof(InvokeTablesRenamedEvent), tables, oldNames, oldPaths);
            this.CremaHost.Debug(eventLog);
            this.OnTablesRenamed(new ItemsRenamedEventArgs<ITable>(authentication, tables, oldNames, oldPaths, dataSet));
            this.Context.InvokeItemsRenamedEvent(authentication, tables, oldNames, oldPaths, dataSet);
        }

        public void InvokeTablesMovedEvent(Authentication authentication, Table[] tables, string[] oldPaths, string[] oldCategoryPaths, CremaDataSet dataSet)
        {
            var eventLog = EventLogBuilder.BuildMany(authentication, this, nameof(InvokeTablesMovedEvent), tables, oldPaths, oldCategoryPaths);
            this.CremaHost.Debug(eventLog);
            this.OnTablesMoved(new ItemsMovedEventArgs<ITable>(authentication, tables, oldPaths, oldCategoryPaths, dataSet));
            this.Context.InvokeItemsMovedEvent(authentication, tables, oldPaths, oldCategoryPaths, dataSet);
        }

        public void InvokeTablesDeletedEvent(Authentication authentication, Table[] tables, string[] oldPaths)
        {
            var dataSet = CremaDataSet.Create(new SignatureDateProvider(authentication.ID));
            var eventLog = EventLogBuilder.BuildMany(authentication, this, nameof(InvokeTablesDeletedEvent), oldPaths);
            this.CremaHost.Debug(eventLog);
            this.OnTablesDeleted(new ItemsDeletedEventArgs<ITable>(authentication, tables, oldPaths, dataSet));
            this.Context.InvokeItemsDeletedEvent(authentication, tables, oldPaths, dataSet);
        }

        public void InvokeTablesStateChangedEvent(Authentication authentication, Table[] tables)
        {
            this.CremaHost.DebugMethodMany(authentication, this, nameof(InvokeTablesStateChangedEvent), tables);
            this.OnTablesStateChanged(new ItemsEventArgs<ITable>(authentication, tables));
        }

        public void InvokeTablesTemplateChangedEvent(Authentication authentication, Table[] tables, CremaDataSet dataSet)
        {
            var eventLog = EventLogBuilder.BuildMany(authentication, this, nameof(InvokeTablesTemplateChangedEvent), tables);
            this.CremaHost.Debug(eventLog);
            this.OnTablesChanged(new ItemsEventArgs<ITable>(authentication, tables, dataSet));
            this.Context.InvokeItemsChangedEvent(authentication, tables, dataSet);
        }

        public void InvokeTablesContentChangedEvent(Authentication authentication, TableContent content, Table[] tables, CremaDataSet dataSet)
        {
            var eventLog = EventLogBuilder.BuildMany(authentication, this, nameof(InvokeTablesContentChangedEvent), tables);
            this.CremaHost.Debug(eventLog);
            this.OnTablesChanged(new ItemsEventArgs<ITable>(authentication, tables));
            this.Context.InvokeItemsChangedEvent(authentication, tables, dataSet);
        }

        public DataBaseRepositoryHost Repository
        {
            get { return this.DataBase.Repository; }
        }

        public CremaHost CremaHost
        {
            get { return this.Context.CremaHost; }
        }

        public DataBase DataBase
        {
            get { return this.Context.DataBase; }
        }

        public CremaDispatcher Dispatcher
        {
            get { return this.Context?.Dispatcher; }
        }

        public IObjectSerializer Serializer => this.DataBase.Serializer;

        public new int Count
        {
            get
            {
                this.Dispatcher?.VerifyAccess();
                return base.Count;
            }
        }

        public event ItemsCreatedEventHandler<ITable> TablesCreated
        {
            add
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesCreated += value;
            }
            remove
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesCreated -= value;
            }
        }

        public event ItemsRenamedEventHandler<ITable> TablesRenamed
        {
            add
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesRenamed += value;
            }
            remove
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesRenamed -= value;
            }
        }

        public event ItemsMovedEventHandler<ITable> TablesMoved
        {
            add
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesMoved += value;
            }
            remove
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesMoved -= value;
            }
        }

        public event ItemsDeletedEventHandler<ITable> TablesDeleted
        {
            add
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesDeleted += value;
            }
            remove
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesDeleted -= value;
            }
        }

        public event ItemsEventHandler<ITable> TablesChanged
        {
            add
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesChanged += value;
            }
            remove
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesChanged -= value;
            }
        }

        public event ItemsEventHandler<ITable> TablesStateChanged
        {
            add
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesStateChanged += value;
            }
            remove
            {
                this.Dispatcher?.VerifyAccess();
                this.tablesStateChanged -= value;
            }
        }

        public new event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                this.Dispatcher?.VerifyAccess();
                base.CollectionChanged += value;
            }
            remove
            {
                this.Dispatcher?.VerifyAccess();
                base.CollectionChanged -= value;
            }
        }

        protected override void ValidateAddNew(string name, string categoryPath, object validation)
        {
            base.ValidateAddNew(name, categoryPath, validation);

            if (NameValidator.VerifyName(name) == false)
                throw new ArgumentException(string.Format(Resources.Exception_InvalidName_Format, name), nameof(name));

            if (validation is Authentication authentication)
            {
                var category = this.GetCategory(categoryPath);
                category.ValidateAccessType(authentication, AccessType.Master);
            }
        }

        protected virtual void OnTablesCreated(ItemsCreatedEventArgs<ITable> e)
        {
            this.tablesCreated?.Invoke(this, e);
        }

        protected virtual void OnTablesRenamed(ItemsRenamedEventArgs<ITable> e)
        {
            this.tablesRenamed?.Invoke(this, e);
        }

        protected virtual void OnTablesMoved(ItemsMovedEventArgs<ITable> e)
        {
            this.tablesMoved?.Invoke(this, e);
        }

        protected virtual void OnTablesDeleted(ItemsDeletedEventArgs<ITable> e)
        {
            this.tablesDeleted?.Invoke(this, e);
        }

        protected virtual void OnTablesStateChanged(ItemsEventArgs<ITable> e)
        {
            this.tablesStateChanged?.Invoke(this, e);
        }

        protected virtual void OnTablesChanged(ItemsEventArgs<ITable> e)
        {
            this.tablesChanged?.Invoke(this, e);
        }

        protected override Table NewItem(params object[] args)
        {
            return new Table();
        }

        private void Sign(Authentication authentication)
        {
            authentication.Sign();
        }

        private void ValidateCreate(Authentication authentication, string categoryPath)
        {
            var category = this.GetCategory(categoryPath);

            if (category == null)
                throw new CategoryNotFoundException(categoryPath);

            category.ValidateAccessType(authentication, AccessType.Master);
        }

        private void ValidateInherit(Authentication authentication, Table table, string newTableName, string categoryPath, bool copyXml)
        {
            table.ValidateAccessType(authentication, AccessType.Master);

            if (this.Contains(newTableName) == true)
                throw new ArgumentException(Resources.Exception_SameTableNameExist, nameof(newTableName));
            if (table.Parent != null)
                throw new InvalidOperationException(Resources.Exception_ChildTableCannotInherit);
            if (table.TemplatedParent != null)
                throw new InvalidOperationException(Resources.Exception_InheritedTableCannotInherit);

            NameValidator.ValidateCategoryPath(categoryPath);

            if (this.Context.Categories.Contains(categoryPath) == false)
                throw new CategoryNotFoundException(categoryPath);

            if (copyXml == true)
            {
                foreach (var item in EnumerableUtility.Friends(table, table.Childs))
                {
                    item.ValidateHasNotBeingEditedType();
                }
            }
        }

        private void ValidateCopy(Authentication authentication, Table table, string newTableName, string categoryPath, bool copyXml)
        {
            table.ValidateAccessType(authentication, AccessType.Master);

            if (this.Contains(newTableName) == true)
                throw new ArgumentException(Resources.Exception_SameTableNameExist, nameof(newTableName));
            if (table.Parent != null)
                throw new InvalidOperationException(Resources.Exception_ChildTableCannotCopy);

            NameValidator.ValidateCategoryPath(categoryPath);

            if (this.Context.Categories.Contains(categoryPath) == false)
                throw new CategoryNotFoundException(categoryPath);

            if (copyXml == true)
            {
                foreach (var item in EnumerableUtility.Friends(table, table.Childs))
                {
                    item.ValidateHasNotBeingEditedType();
                }
            }
        }

        #region ITableCollection

        bool ITableCollection.Contains(string tableName)
        {
            this.Dispatcher?.VerifyAccess();
            return this.Contains(tableName);
        }

        ITable ITableCollection.this[string tableName]
        {
            get
            {
                this.Dispatcher?.VerifyAccess();
                return this[tableName];
            }
        }

        #endregion

        #region IEnumerable

        IEnumerator<ITable> IEnumerable<ITable>.GetEnumerator()
        {
            this.Dispatcher?.VerifyAccess();
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            this.Dispatcher?.VerifyAccess();
            return this.GetEnumerator();
        }

        #endregion

        #region IServiceProvider

        object IServiceProvider.GetService(System.Type serviceType)
        {
            return (this.DataBase as IDataBase).GetService(serviceType);
        }

        #endregion
    }
}
