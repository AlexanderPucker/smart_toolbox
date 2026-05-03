using System;
using System.Collections.Concurrent;

namespace SmartToolbox.Services;

public sealed class ServiceLocator
{
    private static readonly Lazy<ServiceLocator> _instance = new(() => new ServiceLocator());
    public static ServiceLocator Instance => _instance.Value;

    private readonly ConcurrentDictionary<Type, object> _services = new();
    private readonly ConcurrentDictionary<Type, Func<object>> _factories = new();

    private ServiceLocator() { }

    public void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
        where TImplementation : TInterface
    {
        _services[typeof(TInterface)] = instance!;
    }

    public void RegisterSingleton<T>(T instance)
    {
        _services[typeof(T)] = instance!;
    }

    public void RegisterFactory<T>(Func<T> factory)
    {
        _factories[typeof(T)] = () => factory()!;
    }

    public T GetService<T>()
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }

        if (_factories.TryGetValue(typeof(T), out var factory))
        {
            var instance = factory();
            _services[typeof(T)] = instance;
            return (T)instance;
        }

        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    public T? TryGetService<T>()
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        return default;
    }

    public bool IsRegistered<T>()
    {
        return _services.ContainsKey(typeof(T)) || _factories.ContainsKey(typeof(T));
    }

    public void Initialize()
    {
        RegisterSingleton(AIService.Instance);
        RegisterSingleton(TokenCounterService.Instance);
        RegisterSingleton(ConversationManager.Instance);
        RegisterSingleton(ContextWindowManager.Instance);
        RegisterSingleton(ModelRouter.Instance);
        RegisterSingleton(RateLimiter.Instance);
        RegisterSingleton(ToolRegistry.Instance);
        RegisterSingleton(WorkflowEngine.Instance);
        RegisterSingleton(KnowledgeBaseService.Instance);
        RegisterSingleton(UsageStatisticsService.Instance);
        RegisterSingleton(OllamaService.Instance);
        RegisterSingleton(HotkeyService.Instance);
        RegisterSingleton(DataExportService.Instance);
        RegisterSingleton(AgentService.Instance);
        RegisterSingleton(SmartClipboardService.Instance);
        RegisterSingleton(VoiceService.Instance);
    }

    public void Reset()
    {
        _services.Clear();
        _factories.Clear();
    }
}
