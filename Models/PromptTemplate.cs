using System;
using System.Collections.Generic;

namespace SmartToolbox.Models;

public class PromptTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    public override string ToString()
    {
        return Name;
    }
}

public static class DefaultTemplates
{
    public static List<PromptTemplate> GetDefaults()
    {
        return new List<PromptTemplate>
        {
            new PromptTemplate
            {
                Name = "代码审查",
                Category = "编程",
                Description = "对代码进行审查和建议",
                Template = "请审查以下代码，指出潜在问题并提供改进建议：\n\n```{language}\n{code}\n```\n\n请从以下方面分析：\n1. 代码质量和最佳实践\n2. 潜在的性能问题\n3. 安全漏洞\n4. 可维护性建议"
            },
            new PromptTemplate
            {
                Name = "写API文档",
                Category = "编程",
                Description = "为API接口生成文档",
                Template = "请为以下API接口生成完整的文档：\n\n{code}\n\n文档应包含：\n- 功能描述\n- 请求参数说明\n- 响应格式说明\n- 错误码说明\n- 使用示例"
            },
            new PromptTemplate
            {
                Name = "写单元测试",
                Category = "编程",
                Description = "为代码生成单元测试",
                Template = "请为以下代码编写单元测试：\n\n```{language}\n{code}\n```\n\n要求：\n1. 覆盖正常流程\n2. 覆盖异常场景\n3. 包含边界条件测试\n4. 使用{test_framework}框架"
            },
            new PromptTemplate
            {
                Name = "邮件回复",
                Category = "办公",
                Description = "生成专业邮件回复",
                Template = "请帮我回复以下邮件：\n\n原邮件内容：\n{email_content}\n\n回复要点：\n{reply_points}\n\n要求：\n- 语气：{tone}\n- 语言：{language}"
            },
            new PromptTemplate
            {
                Name = "文章改写",
                Category = "写作",
                Description = "改写文章提升质量",
                Template = "请改写以下文章，提升表达质量和逻辑性：\n\n{content}\n\n要求：\n1. 保持原意不变\n2. 优化语言表达\n3. 改进逻辑结构\n4. 字数：{word_count}字左右"
            },
            new PromptTemplate
            {
                Name = "SQL优化",
                Category = "编程",
                Description = "优化SQL查询语句",
                Template = "请优化以下SQL查询：\n\n```sql\n{sql}\n```\n\n当前问题：{problem}\n\n请提供：\n1. 优化后的SQL\n2. 优化说明\n3. 性能提升点"
            }
        };
    }
}
