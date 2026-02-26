# Unity Dev Agent - 技术架构设计

> **文档编号**: UDA-DESIGN-001  
> **版本**: v1.0  
> **状态**: 草案  
> **最后更新**: 2026-02-26

---

## 1. 架构概览

### 1.1 系统架构图

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              Unity Dev Agent                                    │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│   ┌─────────────────────────────────────────────────────────────────────────┐  │
│   │                        Agent Core (Agent Kernel)                        │  │
│   │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │  │
│   │  │   Spec      │  │   ReAct     │  │   State     │  │   Config    │    │  │
│   │  │   Engine    │  │   Engine    │  │   Manager   │  │   Manager   │    │  │
│   │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │  │
│   └─────────────────────────────────────────────────────────────────────────┘  │
│                                    │                                            │
│   ┌─────────────────────────────────────────────────────────────────────────┐  │
│   │                      Module Layer (业务模块层)                           │  │
│   │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐  │  │
│   │  │   Spec       │  │   Code       │  │   Build      │  │  Validation │  │  │
│   │  │   Module     │  │   Module     │  │   Module     │  │   Module    │  │  │
│   │  ├──────────────┤  ├──────────────┤  ├──────────────┤  ├─────────────┤  │  │
│   │  │ - Clarifier  │  │ - Generator  │  │ - Compiler   │  │ - Executor  │  │  │
│   │  │ - SpecGen    │  │ - Fixer      │  │ - Monitor    │  │ - Analyzer  │  │  │
│   │  │ - Confirmer  │  │ - Template   │  │ - Recovery   │  │ - Reporter  │  │  │
│   │  └──────────────┘  └──────────────┘  └──────────────┘  └─────────────┘  │  │
│   └─────────────────────────────────────────────────────────────────────────┘  │
│                                    │                                            │
│   ┌─────────────────────────────────────────────────────────────────────────┐  │
│   │                     Adapter Layer (适配器层)                             │  │
│   │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                    │  │
│   │  │  MCP Client  │  │   LLM Client │  │  Persistence │                    │  │
│   │  │  Adapter     │  │   Adapter    │  │  Adapter     │                    │  │
│   │  └──────────────┘  └──────────────┘  └──────────────┘                    │  │
│   └─────────────────────────────────────────────────────────────────────────┘  │
│                                    │                                            │
└────────────────────────────────────┼────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              External Systems                                   │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│   ┌──────────────┐      ┌──────────────┐      ┌──────────────┐                │
│   │ Unity MCP    │      │   LLM API    │      │   Storage    │                │
│   │   Server     │      │(Claude/GPT)  │      │(SQLite/JSON) │                │
│   └──────────────┘      └──────────────┘      └──────────────┘                │
│          │                     │                     │                         │
│          └─────────────────────┴─────────────────────┘                         │
│                            │                                                    │
│                            ▼                                                    │
│                   ┌──────────────────┐                                         │
│                   │   Unity Editor   │                                         │
│                   │  (Target System) │                                         │
│                   └──────────────────┘                                         │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 技术栈

| 层级 | 组件 | 技术选型 | 说明 |
|------|------|----------|------|
| **Core** | Agent Kernel | .NET 6+ | 核心业务逻辑 |
| **Core** | State Management | SQLite + JSON | 状态持久化 |
| **Module** | Spec Engine | C# + Markdown | Spec生成与解析 |
| **Module** | ReAct Engine | C# | 推理-行动循环 |
| **Adapter** | MCP Client | mcp-dotnet SDK | 与Unity MCP通信 |
| **Adapter** | LLM Client | Anthropic/GPT SDK | AI能力调用 |
| **External** | Unity MCP | Unity-MCP Plugin | Unity端能力 |
| **External** | LLM Service | Claude 3.5 Sonnet | 多模态AI |

---

## 2. 核心组件设计

### 2.1 Agent Kernel (核心引擎)

#### 2.1.1 类图

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           IUnityDevAgent                                │
├─────────────────────────────────────────────────────────────────────────┤
│ + DevelopAsync(req): Task<DevelopmentResult>                            │
│ + GetStatus(): AgentStatus                                              │
│ + ProvideFeedback(feedback): Task                                       │
│ + Terminate(): void                                                     │
└─────────────────────────────────────────────────────────────────────────┘
                                    ▲
                                    │ implements
                                    │
┌─────────────────────────────────────────────────────────────────────────┐
│                          UnityDevAgent                                  │
├─────────────────────────────────────────────────────────────────────────┤
│ - _specModule: SpecModule                                               │
│ - _codeModule: CodeModule                                               │
│ - _buildModule: BuildModule                                             │
│ - _validationModule: ValidationModule                                   │
│ - _reactEngine: ReActEngine                                             │
│ - _stateManager: StateManager                                           │
├─────────────────────────────────────────────────────────────────────────┤
│ + DevelopAsync(req): Task<DevelopmentResult>                            │
│ - ExecutePhase1Spec(req): Task<SpecDocument>                            │
│ - ExecutePhase2Implement(spec): Task<ImplementationResult>              │
└─────────────────────────────────────────────────────────────────────────┘
```

#### 2.1.2 状态机

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                            Agent State Machine                               │
└──────────────────────────────────────────────────────────────────────────────┘

    ┌─────────┐
    │  Idle   │
    └────┬────┘
         │ StartDevelopment
         ▼
    ┌─────────────────────────────────────────────────────────────────────┐
    │                          Phase 1: SPEC                              │
    │  ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐     │
    │  │ Gathering│ → │ Designing│ → │ Confirm  │ → │ Confirmed│     │
    │  │  Req     │    │  Spec    │    │  User    │    │          │     │
    │  └──────────┘    └──────────┘    └──────────┘    └────┬─────┘     │
    └───────────────────────────────────────────────────────┼───────────┘
                                                            │
                                                            ▼
    ┌─────────────────────────────────────────────────────────────────────┐
    │                        Phase 2: IMPLEMENT                           │
    │                                                                     │
    │   ┌─────────┐                                                      │
    │   │  Start  │                                                      │
    │   └────┬────┘                                                      │
    │        │                                                          │
    │        ▼                                                          │
    │   ┌─────────┐    Compile    ┌─────────┐                          │
    │   │Generate │ ────Failed──→ │  Fix    │ ──┐                      │
    │   │  Code   │               │  Code   │   │                      │
    │   └────┬────┘               └─────────┘   │ Max Retry            │
    │        │ Success                           │ Exceeded             │
    │        ▼                                  ▼                      │
    │   ┌─────────┐                        ┌─────────┐                │
    │   │Compile  │                        │ Failed  │                │
    │   │ Check   │                        │         │                │
    │   └────┬────┘                        └─────────┘                │
    │        │ Success                                                  │
    │        ▼                                                          │
    │   ┌─────────┐    Failed     ┌─────────┐                          │
    │   │ Validate│ ─────────────→│  Fix    │ ──┐                      │
    │   │  Runtime│               │  Code   │   │                      │
    │   └────┬────┘               └─────────┘   │ Max Retry            │
    │        │ Success                           │ Exceeded             │
    │        ▼                                  ▼                      │
    │   ┌─────────┐                        ┌─────────┐                │
    │   │Success  │                        │ Failed  │                │
    │   │         │                        │         │                │
    │   └─────────┘                        └─────────┘                │
    │                                                                     │
    └─────────────────────────────────────────────────────────────────────┘
```

---

### 2.2 ReAct Engine (推理引擎)

#### 2.2.1 核心类设计

```csharp
// ReAct循环引擎
public class ReActEngine
{
    private readonly List<IReActStep> _history = new();
    private readonly ILLMClient _llmClient;
    private readonly IToolRegistry _toolRegistry;
    
    public async Task<ReActResult> ExecuteAsync(
        string goal, 
        ReActContext context,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // 1. Observe - 观察当前状态
            var observation = await ObserveAsync(context);
            
            // 2. Think - 推理下一步
            var thought = await ThinkAsync(goal, observation, _history);
            
            // 3. Act - 执行行动
            var action = await DecideActionAsync(thought, _toolRegistry);
            var actionResult = await ExecuteActionAsync(action);
            
            // 4. Reflect - 反思结果
            var reflection = await ReflectAsync(actionResult);
            
            // 记录步骤
            _history.Add(new ReActStep(observation, thought, action, reflection));
            
            // 检查终止条件
            if (ShouldTerminate(reflection))
            {
                return CreateResult(reflection);
            }
        }
        
        return ReActResult.Cancelled();
    }
}

// ReAct步骤记录
public class ReActStep
{
    public Observation Observation { get; set; }
    public Thought Thought { get; set; }
    public Action Action { get; set; }
    public Reflection Reflection { get; set; }
    public DateTime Timestamp { get; set; }
}

// 观察结果
public class Observation
{
    public CompilationStatus CompilationStatus { get; set; }
    public List<LogEntry> Logs { get; set; }
    public byte[] Screenshot { get; set; }
    public SceneState SceneState { get; set; }
}

// 思考结果
public class Thought
{
    public string Analysis { get; set; }           // 当前状态分析
    public string Plan { get; set; }               // 后续计划
    public string NextAction { get; set; }         // 下一步行动
    public double Confidence { get; set; }         // 置信度
}

// 行动定义
public class Action
{
    public string ToolName { get; set; }           // 工具名称
    public object Parameters { get; set; }         // 参数
    public ActionPriority Priority { get; set; }   // 优先级
}

// 反思结果
public class Reflection
{
    public bool IsSuccessful { get; set; }         // 是否成功
    public string Evaluation { get; set; }         // 评估说明
    public bool ShouldContinue { get; set; }       // 是否继续
    public string Adjustment { get; set; }         // 策略调整建议
}
```

#### 2.2.2 Prompt设计

```
# ReAct Prompt Template

You are a Unity development expert. You are working on: {goal}

## Current Context
- Iteration: {iteration}/{maxIterations}
- Previous Actions: {actionHistory}

## Observation
```json
{observation}
```

## Instructions
1. Analyze the observation carefully
2. Determine if the task is complete
3. If not complete, decide the next action
4. Available tools: {availableTools}

## Output Format
```json
{
  "thought": {
    "analysis": "Current state analysis...",
    "plan": "Next steps plan...",
    "confidence": 0.85
  },
  "action": {
    "tool": "tool_name",
    "parameters": {}
  },
  "reflection": {
    "shouldContinue": true,
    "evaluation": "Why continue or stop"
  }
}
```
```

---

### 2.3 Spec Module (规格模块)

#### 2.3.1 组件设计

```csharp
// Spec模块主类
public class SpecModule
{
    private readonly IRequirementClarifier _clarifier;
    private readonly ISpecGenerator _generator;
    private readonly IUserConfirmer _confirmer;
    
    public async Task<SpecDocument> CreateSpecAsync(
        string initialRequirement, 
        IUserInterface ui)
    {
        // 1. 澄清需求
        var clarifiedReq = await _clarifier.ClarifyAsync(
            initialRequirement, ui);
        
        // 2. 生成Spec
        var spec = await _generator.GenerateAsync(clarifiedReq);
        
        // 3. 用户确认
        var confirmedSpec = await _confirmer.ConfirmAsync(spec, ui);
        
        return confirmedSpec;
    }
}

// 需求澄清器
public class RequirementClarifier : IRequirementClarifier
{
    private readonly ILLMClient _llm;
    
    public async Task<ClarifiedRequirement> ClarifyAsync(
        string requirement, IUserInterface ui)
    {
        var questions = await _llm.GenerateQuestionsAsync(requirement);
        var answers = new Dictionary<string, string>();
        
        foreach (var question in questions)
        {
            var answer = await ui.AskAsync(question);
            answers[question.Id] = answer;
        }
        
        return new ClarifiedRequirement(requirement, answers);
    }
}

// Spec生成器
public class SpecGenerator : ISpecGenerator
{
    private readonly ITemplateEngine _template;
    private readonly ILLMClient _llm;
    
    public async Task<SpecDocument> GenerateAsync(
        ClarifiedRequirement requirement)
    {
        // 使用模板 + LLM生成完整Spec
        var template = _template.Load("tool-spec-template.md");
        var prompt = BuildPrompt(requirement, template);
        var specContent = await _llm.GenerateAsync(prompt);
        
        return SpecDocument.Parse(specContent);
    }
}
```

#### 2.3.2 Spec文档结构

```yaml
# spec-document-schema.yaml
spec_document:
  version: "1.0"
  metadata:
    title: "工具名称"
    description: "工具描述"
    author: "Agent/用户"
    created_at: "2026-02-26"
  
  functional_spec:
    inputs: []
    outputs: {}
    features: []
    ui_layout: {}
  
  technical_spec:
    classes: []
    algorithms: {}
    dependencies: []
  
  validation_spec:
    criteria: []
    log_rules: {}
    visual_checks: []
```

---

### 2.4 Code Module (代码模块)

#### 2.4.1 代码生成器

```csharp
public class CodeGenerator
{
    private readonly ICodeTemplateRepository _templates;
    private readonly ILLMClient _llm;
    
    public async Task<GeneratedCode> GenerateAsync(
        SpecDocument spec, 
        CodeGenerationOptions options)
    {
        // 1. 选择模板
        var template = _templates.GetTemplate(spec.ToolType);
        
        // 2. 构建Prompt
        var prompt = $@"
Generate Unity C# code based on the following specification:

Template:
{template.Content}

Specification:
{spec.ToMarkdown()}

Requirements:
- Use namespace: {options.Namespace}
- Follow naming conventions: {options.NamingConvention}
- Include XML documentation
- Add error handling

Output the complete code file content only.
";
        
        // 3. 生成代码
        var code = await _llm.GenerateAsync(prompt);
        
        // 4. 后处理（格式化、语法检查等）
        code = PostProcess(code);
        
        return new GeneratedCode(spec.FilePath, code);
    }
}
```

#### 2.4.2 代码修复器

```csharp
public class CodeFixer
{
    private readonly ILLMClient _llm;
    private readonly IFixStrategy _strategy;
    
    public async Task<FixedCode> FixAsync(
        string originalCode, 
        CompilationError[] errors)
    {
        switch (_strategy)
        {
            case FixStrategy.Incremental:
                return await FixIncrementalAsync(originalCode, errors);
            case FixStrategy.FullRegeneration:
                return await FixFullAsync(originalCode, errors);
            default:
                throw new NotSupportedException();
        }
    }
    
    private async Task<FixedCode> FixIncrementalAsync(
        string code, CompilationError[] errors)
    {
        // 逐个修复错误
        foreach (var error in errors)
        {
            var fix = await GenerateFixAsync(code, error);
            code = ApplyFix(code, fix);
        }
        return new FixedCode(code);
    }
}
```

---

### 2.5 Build Module (编译模块)

#### 2.5.1 编译监控器

```csharp
public class CompilationMonitor
{
    private readonly IMcpClient _mcp;
    private readonly IStateManager _state;
    
    public async Task<CompilationResult> CompileAsync(
        string filePath, 
        string code)
    {
        // 1. 保存当前状态
        _state.SaveState(new CompilationState 
        { 
            FilePath = filePath, 
            Code = code,
            StartTime = DateTime.UtcNow 
        });
        
        // 2. 写入代码文件
        await _mcp.InvokeToolAsync("script-update-or-create", new {
            path = filePath,
            code = code
        });
        
        // 3. 触发编译
        await _mcp.InvokeToolAsync("asset-refresh");
        
        // 4. 等待编译完成
        var result = await WaitForCompilationAsync(timeout: TimeSpan.FromSeconds(60));
        
        // 5. 获取编译结果
        if (result.Status == CompilationStatus.Failed)
        {
            result.Errors = await GetCompilationErrorsAsync();
        }
        
        return result;
    }
    
    private async Task<CompilationResult> WaitForCompilationAsync(
        TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            var state = await _mcp.GetEditorStateAsync();
            
            if (!state.IsCompiling)
            {
                return new CompilationResult
                {
                    Status = state.CompilationFailed 
                        ? CompilationStatus.Failed 
                        : CompilationStatus.Success,
                    Duration = DateTime.UtcNow - startTime
                };
            }
            
            await Task.Delay(500);
        }
        
        return CompilationResult.Timeout();
    }
}
```

#### 2.5.2 Domain Reload恢复机制

```csharp
[InitializeOnLoad]
public class DomainReloadRecovery
{
    static DomainReloadRecovery()
    {
        EditorApplication.update += OnEditorUpdate;
    }
    
    static void OnEditorUpdate()
    {
        // 检查是否有未完成的Agent任务
        if (AgentStatePersistence.HasPendingTask())
        {
            var state = AgentStatePersistence.LoadState();
            
            // 恢复Agent执行
            Task.Run(async () =>
            {
                var agent = new UnityDevAgent();
                await agent.ResumeAsync(state);
            });
        }
        
        EditorApplication.update -= OnEditorUpdate;
    }
}
```

---

### 2.6 Validation Module (验证模块)

#### 2.6.1 运行时验证器

```csharp
public class RuntimeValidator
{
    private readonly IMcpClient _mcp;
    private readonly IScreenshotAnalyzer _screenshotAnalyzer;
    private readonly ILogAnalyzer _logAnalyzer;
    
    public async Task<ValidationResult> ValidateAsync(
        ValidationSpec spec)
    {
        var result = new ValidationResult();
        
        // 1. 进入PlayMode
        await _mcp.InvokeToolAsync("editor-set-state", new {
            state = "playmode",
            action = "start"
        });
        
        try
        {
            // 2. 等待工具执行
            await Task.Delay(spec.ExecutionTimeout);
            
            // 3. 截图验证
            if (spec.RequiresScreenshot)
            {
                var screenshot = await CaptureScreenshotAsync(spec.ViewType);
                result.VisualValidation = await _screenshotAnalyzer
                    .AnalyzeAsync(screenshot, spec.VisualCriteria);
            }
            
            // 4. 日志验证
            if (spec.RequiresLogCheck)
            {
                var logs = await _mcp.InvokeToolAsync("console-get-logs");
                result.LogValidation = _logAnalyzer.Analyze(logs, spec.LogCriteria);
            }
            
            // 5. 场景状态验证
            if (spec.RequiresSceneCheck)
            {
                result.SceneValidation = await ValidateSceneAsync(spec.SceneCriteria);
            }
        }
        finally
        {
            // 6. 退出PlayMode
            await _mcp.InvokeToolAsync("editor-set-state", new {
                state = "playmode",
                action = "stop"
            });
        }
        
        result.IsSuccessful = result.AllPassed();
        return result;
    }
}
```

#### 2.6.2 截图分析器

```csharp
public class ScreenshotAnalyzer : IScreenshotAnalyzer
{
    private readonly ILLMClient _llm;
    
    public async Task<VisualValidationResult> AnalyzeAsync(
        byte[] screenshot, 
        VisualCriteria criteria)
    {
        var prompt = $@"
Analyze this Unity screenshot and check if it meets the following criteria:

Criteria:
{criteria.Description}

Required elements:
{string.Join("\n", criteria.RequiredElements)}

Provide a detailed analysis in the following format:
- Overall match score (0-100)
- Which elements are present
- Which elements are missing
- Any anomalies or issues
";
        
        var result = await _llm.AnalyzeImageAsync(screenshot, prompt);
        return ParseResult(result);
    }
}
```

---

## 3. 数据流设计

### 3.1 开发流程数据流

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   User      │────▶│    Agent    │────▶│    LLM      │────▶│   Spec      │
│ Requirement │     │   Kernel    │     │   Service   │     │  Document   │
└─────────────┘     └─────────────┘     └─────────────┘     └──────┬──────┘
                                                                   │
                                                                   ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│    User     │◀────│  Confirm    │◀────│    UI       │◀────│  Present    │
│  Approval   │     │             │     │  Interface  │     │             │
└──────┬──────┘     └─────────────┘     └─────────────┘     └─────────────┘
       │
       ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Phase 2   │────▶│   ReAct     │────▶│   Code      │────▶│   Unity     │
│   Start     │     │   Loop      │     │  Generation │     │   MCP       │
└─────────────┘     └──────┬──────┘     └─────────────┘     └──────┬──────┘
                           │                                        │
                           │         ┌─────────────┐               │
                           │◀────────│   Compile   │◀──────────────┘
                           │         │   Result    │
                           │         └──────┬──────┘
                           │                │
                           │    ┌───────────┴───────────┐
                           │    │                       │
                           │    ▼                       ▼
                           │ ┌──────┐             ┌──────────┐
                           │ │ Fix  │             │ Validate │
                           │ │ Code │             │ Runtime  │
                           │ └──┬───┘             └────┬─────┘
                           │    │                      │
                           └────┴──────────────────────┘
                                  │
                                  ▼
                           ┌─────────────┐
                           │   Success   │
                           │    or       │
                           │   Failed    │
                           └──────┬──────┘
                                  │
                                  ▼
                           ┌─────────────┐
                           │   Deliver   │
                           │   Result    │
                           └─────────────┘
```

### 3.2 状态持久化设计

```yaml
# state-schema.yaml
agent_state:
  version: "1.0"
  session_id: "uuid"
  created_at: "timestamp"
  updated_at: "timestamp"
  
  current_phase: "spec|implement|validate"
  current_iteration: 0
  max_iterations: 10
  
  requirement:
    original: "string"
    clarified: "string"
    answers: []
  
  spec_document:
    content: "markdown"
    confirmed: false
  
  implementation:
    current_code: "string"
    code_history: []
    compilation_attempts: []
  
  validation:
    results: []
    screenshots: []
    logs: []
```

---

## 4. 接口设计

### 4.1 对外API

```csharp
// 主入口接口
public interface IUnityDevAgent : IDisposable
{
    /// <summary>
    /// 启动开发任务
    /// </summary>
    Task<DevelopmentResult> DevelopAsync(
        DevelopmentRequest request, 
        IProgress<DevelopmentProgress> progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取当前状态
    /// </summary>
    AgentStatus GetStatus();
    
    /// <summary>
    /// 暂停任务（等待用户输入）
    /// </summary>
    Task PauseAsync(string reason);
    
    /// <summary>
    /// 恢复任务
    /// </summary>
    Task ResumeAsync(UserFeedback feedback);
    
    /// <summary>
    /// 终止任务
    /// </summary>
    void Terminate();
}

// 开发请求
public class DevelopmentRequest
{
    public string Requirement { get; set; }
    public DevelopmentOptions Options { get; set; }
}

// 开发结果
public class DevelopmentResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public SpecDocument Spec { get; set; }
    public string GeneratedCodePath { get; set; }
    public ValidationResult ValidationResult { get; set; }
    public int Iterations { get; set; }
    public TimeSpan Duration { get; set; }
}
```

### 4.2 MCP工具调用封装

```csharp
public interface IMcpToolInvoker
{
    Task<ToolResult<T>> InvokeAsync<T>(
        string toolName, 
        object parameters,
        TimeSpan? timeout = null);
    
    Task<bool> WaitForCompletion(
        string requestId, 
        TimeSpan timeout);
    
    event EventHandler<ToolNotification> OnNotification;
}

// 工具调用示例
public class UnityTools
{
    private readonly IMcpToolInvoker _invoker;
    
    public async Task UpdateScriptAsync(string path, string code)
    {
        var result = await _invoker.InvokeAsync<object>(
            "script-update-or-create",
            new { path, code });
        
        if (!result.Success)
        {
            throw new ToolInvocationException(result.Error);
        }
    }
    
    public async Task<byte[]> CaptureScreenshotAsync(string viewType)
    {
        var result = await _invoker.InvokeAsync<ScreenshotResult>(
            $"screenshot-{viewType}",
            new { width = 1920, height = 1080 });
        
        return result.Data.ImageData;
    }
}
```

---

## 5. 部署架构

### 5.1 独立进程模式 (推荐)

```
┌─────────────────────────────────────────────────────────────────┐
│                        User Machine                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────┐      ┌─────────────────────────────┐  │
│  │   Unity Dev Agent   │      │      Unity Editor           │  │
│  │   (Console App)     │◀────▶│  ┌───────────────────────┐  │  │
│  │                     │ MCP  │  │   Unity MCP Plugin    │  │  │
│  │  ┌───────────────┐  │      │  └───────────────────────┘  │  │
│  │  │   Agent Core  │  │      └─────────────────────────────┘  │
│  │  └───────────────┘  │                                       │
│  │         │           │                                       │
│  │         ▼           │                                       │
│  │  ┌───────────────┐  │                                       │
│  │  │   LLM Client  │────▶  Claude API / GPT API               │
│  │  └───────────────┘  │                                       │
│  └─────────────────────┘                                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Editor插件模式

```
┌─────────────────────────────────────────────────────────────────┐
│                      Unity Editor                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                 Unity MCP Plugin                          │ │
│  ├───────────────────────────────────────────────────────────┤ │
│  │                                                           │ │
│  │  ┌─────────────────────────────────────────────────────┐  │ │
│  │  │              Unity Dev Agent (Editor)               │  │ │
│  │  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌────────┐  │  │ │
│  │  │  │  Spec    │ │  Code    │ │  Build   │ │ Validate│  │  │ │
│  │  │  │  Module  │ │  Module  │ │  Module  │ │ Module  │  │  │ │
│  │  │  └──────────┘ └──────────┘ └──────────┘ └────────┘  │  │ │
│  │  └─────────────────────────────────────────────────────┘  │ │
│  │                            │                              │ │
│  │                            ▼                              │ │
│  │                   ┌──────────────┐                        │ │
│  │                   │   LLM Client │──▶  Claude API        │ │
│  │                   └──────────────┘                        │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. 待确认设计决策

### 6.1 架构决策

| ID | 决策项 | 选项A | 选项B | 推荐 |
|----|--------|-------|-------|------|
| **D1** | **部署模式** | 独立进程 | Editor插件 | **A** |
| **D2** | **状态存储** | SQLite | JSON文件 | **A** |
| **D3** | **LLM调用** | 同步阻塞 | 异步流式 | **B** |
| **D4** | **重试策略** | 固定间隔 | 指数退避 | **B** |

### 6.2 技术决策

| ID | 决策项 | 选项A | 选项B | 选项C | 推荐 |
|----|--------|-------|-------|-------|------|
| **D5** | **日志级别** | Debug | Info | Warning | **B** |
| **D6** | **截图格式** | PNG | JPEG | WebP | **A** |
| **D7** | **超时策略** | 固定超时 | 动态超时 | 无超时 | **B** |

### 6.3 产品决策

| ID | 决策项 | 选项A | 选项B | 推荐 |
|----|--------|-------|-------|------|
| **D8** | **默认迭代次数** | 5 | 10 | **B** |
| **D9** | **并发任务** | 单任务 | 多任务队列 | **A** |
| **D10** | **失败处理** | 自动终止 | 转人工 | **B** |

**需要您确认以上决策，特别是 D1（部署模式）、D3（LLM调用方式）和 D8（默认迭代次数）**

---

## 7. 附录

### 7.1 代码模板示例

```csharp
// Editor工具模板
namespace {Namespace}.Editor
{
    public class {ToolName}Window : EditorWindow
    {
        [MenuItem("Tools/{ToolName}")]
        public static void ShowWindow()
        {
            GetWindow<{ToolName}Window>("{ToolName}");
        }
        
        private void OnGUI()
        {
            // Generated UI
        }
        
        private void OnEnable()
        {
            Debug.Log("[{ToolName}] Initialized");
        }
    }
}
```

### 7.2 错误处理策略

```csharp
public enum ErrorHandlingStrategy
{
    Retry,          // 重试
    Fallback,       // 降级
    Escalate,       // 升级（转人工）
    Abort           // 终止
}
```

### 7.3 监控指标

| 指标名 | 类型 | 说明 |
|--------|------|------|
| agent_iterations_total | Counter | 总迭代次数 |
| agent_compilation_failures | Counter | 编译失败次数 |
| agent_development_duration | Histogram | 开发耗时分布 |
| agent_llm_tokens_used | Counter | LLM Token消耗 |
