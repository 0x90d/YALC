using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace YetAnotherLosslessCutter.MVVM
{
    public abstract class ViewModelBase : INotifyPropertyChanged, IDataErrorInfo
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        protected void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

        [XmlIgnore]
        string IDataErrorInfo.Error => throw new NotImplementedException();

        [XmlIgnore]
        string IDataErrorInfo.this[string columnName] => Verify(columnName);

        [Browsable(false), XmlIgnore]
        public  virtual bool HasError => false;

        protected virtual string Verify(string columnName) => string.Empty;
        protected void HasErrorUpdated() => OnPropertyChanged(nameof(HasError));

        protected bool Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            return Set(propertyName, ref field, newValue);
        }
        protected bool Set<T>(string propertyName, ref T field, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;

            // ReSharper disable ExplicitCallerInfoArgument
            OnPropertyChanged(propertyName);
            // ReSharper restore ExplicitCallerInfoArgument

            return true;
        }
    }
}
