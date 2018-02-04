using System;

/// <summary>
/// Creates an attribute to mark methods to be part of a context menu.
/// This attribute can be declared by marking [contextMethod(contextTitle)] above a method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ContextMethod : Attribute
{
	public string contextTitle;

	public ContextMethod(string menuEntryTitle)
	{
		this.contextTitle = menuEntryTitle;
	}
}
