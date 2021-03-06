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

using Ntreev.Crema.Data.Xml.Schema;
using Ntreev.Crema.Services;
using Ntreev.Library;
using Ntreev.ModernUI.Framework;
using Ntreev.ModernUI.Framework.DataGrid.Controls;
using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Ntreev.Crema.Client.Framework.Dialogs.ViewModels;
using Ntreev.Crema.Data;
using Ntreev.ModernUI.Framework.DataGrid;
using Xceed.Wpf.DataGrid;

namespace Ntreev.Crema.Client.Tables.Dialogs.Views
{
    public partial class TemplateView : UserControl
    {
        [Import]
        private IAppConfiguration configs = null;

        public static DependencyProperty DomainProperty = DependencyProperty.Register(nameof(Domain), typeof(IDomain), typeof(TemplateView));

        public TemplateView()
        {
            BindingOperations.SetBinding(this, DomainProperty, new Binding("Domain") { Mode = BindingMode.OneWay, });
        }

        public IDomain Domain
        {
            get { return (IDomain)this.GetValue(DomainProperty); }
            set { this.SetValue(DomainProperty, value); }
        }

        private void PART_DataGridControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                var gridControl = sender as ModernDataGridControl;
                gridControl.OpenCodeEditorOnCell += GridControl_OpenCodeEditorOnCell;
                if (gridControl.Columns[CremaSchema.ID] is ColumnBase column)
                    column.Visible = false;
                this.configs.Update(this);
                if (this.Settings != null && Keyboard.IsKeyDown(Key.LeftShift) == false)
                    gridControl.LoadUserSettings(this.Settings, Xceed.Wpf.DataGrid.Settings.UserSettings.All);
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void PART_DataGridControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var gridControl = sender as ModernDataGridControl;
            this.Settings = new Xceed.Wpf.DataGrid.Settings.SettingsRepository();
            gridControl.SaveUserSettings(this.Settings, Xceed.Wpf.DataGrid.Settings.UserSettings.All);
            this.configs.Commit(this);
        }

        private void GridControl_OpenCodeEditorOnCell(object sender, DataCellEventArgs e)
        {
            try
            {
                var getEditingContent = new Func<DataCell, object>((c) =>
                {
                    if (c is IEditingContent content)
                    {
                        return content.EditingContent;
                    }

                    throw new ArgumentException(nameof(c));
                });
                var dataCell = getEditingContent(e.Cell);
                var saveAction = new Action<string>((text) =>
                {
                    if (e.Cell is IEditingContent content)
                    {
                        content.EditingContent = text;
                    }
                });
                var subTitle = (this.Domain.Source as CremaTemplate)?.Name ?? "";
                var dialog = new CodeEditorViewModel(this.configs, saveAction, subTitle) { Text = (dataCell ?? "").ToString() };
                if (dialog.ShowDialog() == true)
                {
                    saveAction?.Invoke(dialog.Text);
                };
            }
            catch (Exception ex)
            {
                AppMessageBox.ShowError(ex);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //this.tableName.Focus();
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox.Focus();
        }

        private void TableName_Error(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                this.buttons.IsEnabled = false;
            }
            else if (e.Action == ValidationErrorEventAction.Removed)
            {
                this.buttons.IsEnabled = true;
            }
        }

        [ConfigurationProperty("settings")]
        private Xceed.Wpf.DataGrid.Settings.SettingsRepository Settings
        {
            get; set;
        }

        private async void PART_DataGridControl_RowDrop(object sender, ModernDragEventArgs e)
        {
            e.Handled = true;

            var data = e.Data;
            var item = e.Item;
            if (data.GetDataPresent(typeof(int)) == true)
            {
                var index = (int)data.GetData(typeof(int));
                var authenticator = this.Domain.GetService(typeof(Authenticator)) as Authenticator;
                try
                {
                    var domain = this.Domain;
                    await domain.Dispatcher.InvokeAsync(() => domain.SetRow(authenticator, item, CremaSchema.Index, index));
                }
                catch (Exception ex)
                {
                    AppMessageBox.ShowError(ex);
                }
            }
        }
    }
}
