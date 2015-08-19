# raven-uwp

UWP (Universal Windows Platform) client for [Sentry](https://www.getsentry.com/welcome/).

Compatible with Windows 10 apps (Desktop, Mobile, Xbox One and IoT). Also compatible with Windows 8.1 using a linked assembly (albeit only using common APIs).


## Installation

Clone this repo and build it so you can reference the .dll or add the `RavenUWP`/`RavenUWP.Win81` project in your app. Requires Visual Studio 2015.

Will be a NuGet package soon.


## Getting started

1. If you haven't already, [sign up for Sentry](https://www.getsentry.com/signup/). There's numerous plans, including a free tier to get started with.
2. Get your DSN from your project's settings
3. In your application's `App.xaml.cs` file, initialize `RavenClient` _before_ `InitializeComponent()`
```csharp
public App()
{
    // Create your Sentry DSN
    Dsn dsn = new Dsn("http://public:secret@example.com/projectId");

    // Initialize the client with the DSN. Optionally you can choose not to handle unhandled exceptions
    // automatically (default behavior is true)
    RavenClient.InitializeAsync(dsn);

    // The rest of your App() constructor here
    this.InitializeComponent();

    // ...
}
```
4. In your `OnLaunched()` and `OnActivated()` handlers, you can also configure `RavenClient` to capture unobserved async exceptions
```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    RavenClient.Instance.RegisterAsyncContextHandler();

    // The rest of your `OnLaunched() functionality here
    // ...
}

protected override void OnActivated(IActivatedEventArgs args)
{
    // Register an AsyncContextHandler and capture unobserved async exceptions
    RavenClient.Instance.RegisterAsyncContextHandler();

    base.OnActivated(args);
}
```

If you had chosen *not* to capture unhandled exceptions, you'll need to capture them manually (see next section)

Otherwise that's it! Raven will now send all unhandled exceptions to Sentry.


## Setting the user

You can include user information by default with each exception if you have any.
```csharp
RavenClient.Instance.SetUser("USERID", "USERNAME", "useremail@example.com");
```


## Capturing exceptions

After you have called `RavenClient.InitializeAsync(...)`, the client instance is now available at `RavenClient.Instance`.

To capture an exception:
```csharp
try
{
    DoSomethingBad();
}
catch (Exception ex)
{
    RavenUWP.RavenClient.Instance.CaptureExceptionAsync(ex);
}
```

By default any exceptions captured this way are stored in a temporary folder and sent the next time the app is launched. To send the request immediately:
```csharp
RavenUWP.RavenClient.Instance.CaptureExceptionAsync(ex, true);
```

You can also change the LogLevel from the default "Error" by specifying it in the overload:
```csharp
RavenUWP.RavenClient.Instance.CaptureExceptionAsync(ex, true, RavenUWP.RavenLogLevel.Warning);
```

Each request will optionally allow arbitrary indexed [tags](https://docs.getsentry.com/hosted/tagging/) and arbitrary extra data to be sent with the request. Raven will automatically add device, OS, page information and more, so you should focus on setting variables to help debug this particular exception.
```csharp
var tags = new Dictionary<string, string>()
{
    { "Class Name", typeof(MyClass).FullName }
};

var extra = new Dictionary<string, object>()
{
    { "sender", sender.GetType().FullName }
};

RavenUWP.RavenClient.Instance.CaptureExceptionAsync(ex, false, RavenUWP.RavenLogLevel.Error, tags, extra);
```


## Capturing messages

You can send non-exception messages to Sentry as well.
```csharp
RavenUWP.RavenClient.Instance.CaptureMessageAsync("This is not an error!");
```

The method supports the same overloads as `CaptureExceptionAsync()` for sending behavior, level, tags and extra data. The default `RavenLogLevel` is set to `Info`.


## Additional data

You can change the default logger name from "root"
```csharp
RavenUWP.RavenClient.Instance.Logger = "mylogger";
```

If you have any tags or extra data you want to include with every request, you can add those to the client.

```csharp
var ravenClient = RavenUWP.RavenClient.Instance;

ravenClient.DefaultTags = new Dictionary<string, string>();
ravenClient.DefaultTags["Entry Point"] = "Protocol";

ravenClient.DefaultExtra = new Dictionary<string, object>();
ravenClient.DefaultExtra["First App Launch"] = true;
```
