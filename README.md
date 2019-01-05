### This is an add-in for [Fody](https://github.com/Fody/Fody/) [![Build status](https://ci.appveyor.com/api/projects/status/1xenxkw082fqnku2?svg=true)](https://ci.appveyor.com/project/tom-englert/autoproperties-fody) [![NuGet Status](http://img.shields.io/nuget/v/AutoProperties.Fody.svg?style=flat-square)](https://www.nuget.org/packages/AutoProperties.Fody)
![Icon](package_icon.png)

this add-in gives you extended control over auto-properties, like directly accessing the backing field or intercepting getters and setters.

### [Intercepting getter and setter of auto-properties](PropertyInterception.md)

Intercepting the property accessors is useful when e.g. writing a class that provides 
transparent access to store values like e.g. configuration store via properties. 
Instead of implementing every property as reading or writing the store value, 
you can simply add auto-properties for every item and implement reading and writing once in the interceptor.


Instead of 
```C#
public class MyConfiguration
{
    public string Value1
    {
        get => ConfigurationManager.AppSettings[nameof(Value1)];
        set => ConfigurationManager.AppSettings[nameof(Value1)] = value;
    }
    public string Value2
    {
        get => ConfigurationManager.AppSettings[nameof(Value2)];
        set => ConfigurationManager.AppSettings[nameof(Value2)] = value;
    }
}
```
you can write 
```C#
public class MyConfiguration
{
    [GetInterceptor]
    private object GetValue(string name) => ConfigurationManager.AppSettings[name];
    [SetInterceptor]
    private void SetValue(string name, object value) => ConfigurationManager.AppSettings[name] = value?.ToString();

    public string Value1 { get; set; }
    public string Value2 { get; set; }
}
```

Especially if there are many properties, you will save a lot of annoying and error prone typing.

[Click here](PropertyInterception.md) to read the detailed documentation on how to use property interception.

### [Accessing the backing field](BackingFieldAccess.md)

Usually there is no need to access the backing field of an auto-property, because the property setter does just write to the backing field - no more an no less.<para/>

However the story is different when some [Fody](https://github.com/Fody/Fody/) plug-in has extended the setter of auto-properties, like e.g. [Fody.PropertyChanged](https://github.com/Fody/PropertyChanged) does.

When assigning a value to the property, the now modified setter calls `OnPropertyChanged`, which is virtual by default.
If you do this from within the constructor, the constructor has not yet finished, any event handlers assigned in the constructor or code in the overwritten `OnPropertyChanged` method will work on an only partial initialized object, 
which can easily lead to crashes if the event handler is not aware of this. This is why you get e.g. the [CA2214](https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2214-do-not-call-overridable-methods-in-constructors) warning.

With old-style properties you can just bypass this problem by initializing the backing field instead of the property, but with auto-properties you have no chance to do so.

With this Fody add-in you can control if the property setter for auto-properties should be bypassed, and replaced by code that sets just the backing field.

#### Sample
This sample illustrates the problem: Even though there is a null guard in the constructor of the derived class, calling `new Derived(new List<string>())` will 
crash in `OnPropertyChanged` with a `NullReferenceException`:
```C#
public class Class : ObservableObject
{
    public Class(string property1, string property2)
    {
        Property1 = property1;
        Property2 = property2;
    }

    public string Property1 { get; set; }
    public string Property2 { get; set; }
}

public class Derived : Class
{
    private readonly IList<string> _changes;

    public Derived(IList<string> changes)
        : base("Test1", "Test2")
    {
        if (changes == null)
            throw new ArgumentNullException(nameof(changes));

        _changes = changes;
    }

    protected override void OnPropertyChanged(string propertyName)
    {
        _changes.Add(propertyName);

        base.OnPropertyChanged(propertyName);
    }
}
```

[Click here](BackingFieldAccess.md) to read the detailed documentation on how to access the backing field of an auto-property.

## Icon

Designed by [VisualPharm](http://www.visualpharm.com/)
