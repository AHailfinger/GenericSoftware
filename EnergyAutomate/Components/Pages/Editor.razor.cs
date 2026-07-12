using EnergyAutomate.BlazorMonaco;
using EnergyAutomate.BlazorMonaco.Bridge;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace EnergyAutomate.Components.Pages
{
    public partial class Editor
    {
        [Inject]
        [AllowNull]
        private RuntimeCodeTemplateStore TemplateStore { get; set; }

        [Inject]
        [AllowNull]
        private RoslynCodeFactory RoslynCodeFactory { get; set; }

        [Inject]
        [AllowNull]
        private ILogger<Editor> Logger { get; set; }

        [AllowNull]
        private StandaloneCodeEditor _editor;

        [AllowNull]
        private Sidebar docSidebar;

        private List<CodeTemplateViewModel> Templates { get; set; } = [];

        private IEnumerable<NavItem>? SidebarItems { get; set; }

        private string? SelectedTemplateKey { get; set; }

        [SupplyParameterFromQuery(Name = "template")]
        private string? QueryTemplateKey { get; set; }

        private CodeTemplateValidationResult? ValidationResult { get; set; }

        private string StatusText { get; set; } = "Select a template.";

        private bool IsEditorInitialized { get; set; }

        private bool CanEdit => IsEditorInitialized && !string.IsNullOrWhiteSpace(SelectedTemplateKey);

        protected override void OnInitialized()
        {
            Templates = TemplateStore.GetTemplates().ToList();
            SelectedTemplateKey = GetInitialTemplateKey();
            StatusText = SelectedTemplateKey is null ? "No templates available." : "Ready.";
        }

        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrWhiteSpace(QueryTemplateKey)
                && !string.Equals(SelectedTemplateKey, QueryTemplateKey, StringComparison.OrdinalIgnoreCase))
            {
                SelectedTemplateKey = QueryTemplateKey;
                ValidationResult = null;

                if (IsEditorInitialized)
                {
                    await LoadTemplateIntoEditorAsync(QueryTemplateKey);
                }
            }
        }

        private Task<Sidebar2DataProviderResult> Sidebar2DataProvider(Sidebar2DataProviderRequest request)
        {
            SidebarItems ??= BuildSidebarItems();

            return Task.FromResult(request.ApplyTo(SidebarItems));
        }

        private IEnumerable<NavItem> BuildSidebarItems()
        {
            var items = new List<NavItem>();

            foreach (var topicGroup in Templates.GroupBy(template => template.Topic).OrderBy(group => group.Key))
            {
                var topicId = $"topic-{topicGroup.Key}";
                items.Add(new NavItem
                {
                    Id = topicId,
                    Text = topicGroup.Key,
                    IconName = GetTopicIcon(topicGroup.Key),
                    IconColor = GetTopicIconColor(topicGroup.Key)
                });

                foreach (var template in topicGroup.OrderBy(item => item.DisplayName))
                {
                    items.Add(new NavItem
                    {
                        Id = template.Key,
                        Href = $"/editor?template={Uri.EscapeDataString(template.Key)}",
                        Text = template.IsModified ? $"{template.DisplayName} *" : template.DisplayName,
                        IconName = IconName.Dash,
                        ParentId = topicId
                    });
                }
            }

            return items;
        }

        private static IconName GetTopicIcon(string topic)
        {
            return topic switch
            {
                "Calculation" => IconName.Calculator,
                "Adjustment" => IconName.Sliders,
                "Distribution" => IconName.Diagram3Fill,
                _ => IconName.CodeSlash
            };
        }

        private static IconColor GetTopicIconColor(string topic)
        {
            return topic switch
            {
                "Calculation" => IconColor.Primary,
                "Adjustment" => IconColor.Warning,
                "Distribution" => IconColor.Success,
                _ => IconColor.Secondary
            };
        }

        private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
        {
            var selectedTemplate = GetSelectedTemplate();

            return new StandaloneEditorConstructionOptions
            {
                Language = selectedTemplate?.Language ?? "csharp",
                Theme = "vs-dark",
                GlyphMargin = true,
                AutomaticLayout = true,
                Value = selectedTemplate?.CurrentCode ?? string.Empty
            };
        }

        private async Task EditorOnDidInit()
        {
            IsEditorInitialized = true;

            await _editor.AddCommand((int)KeyMod.CtrlCmd | (int)KeyCode.KeyH, (args) =>
            {
                Logger.LogTrace("Ctrl+H editor command triggered");
            });

            await _editor.AddCommand((int)KeyMod.CtrlCmd | (int)KeyCode.KeyS, async (args) =>
            {
                await SaveCurrentTemplateAsync();
            });

            if (SelectedTemplateKey is not null)
            {
                await LoadTemplateIntoEditorAsync(SelectedTemplateKey);
            }
        }

        private void OnContextMenu(EditorMouseEvent eventArg)
        {
            Logger.LogTrace("Editor context menu: {Event}", JsonSerializer.Serialize(eventArg));
        }

        private async Task SelectTemplateAsync(string templateKey)
        {
            if (string.Equals(SelectedTemplateKey, templateKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SelectedTemplateKey = templateKey;
            ValidationResult = null;
            await LoadTemplateIntoEditorAsync(templateKey);
        }

        private async Task LoadTemplateIntoEditorAsync(string templateKey)
        {
            var template = TemplateStore.GetTemplate(templateKey);
            if (template is null)
            {
                StatusText = $"Template '{templateKey}' not found.";
                return;
            }

            Templates = TemplateStore.GetTemplates().ToList();

            if (IsEditorInitialized)
            {
                await _editor.SetValue(template.CurrentCode);
            }

            StatusText = $"Loaded {template.DisplayName}.";
            Logger.LogTrace("Code template {TemplateKey} loaded", template.Key);
        }

        private async Task SaveCurrentTemplateAsync()
        {
            if (!CanEdit || SelectedTemplateKey is null)
            {
                return;
            }

            var code = await _editor.GetValue();
            var validation = RoslynCodeFactory.Validate(code);
            ValidationResult = validation;

            if (!validation.Success)
            {
                StatusText = "Validation failed. Template was not saved.";
                return;
            }

            TemplateStore.SaveTemplate(SelectedTemplateKey, code);
            Templates = TemplateStore.GetTemplates().ToList();
            SidebarItems = BuildSidebarItems();
            StatusText = "Template saved.";
        }

        private async Task ResetCurrentTemplateAsync()
        {
            if (!CanEdit || SelectedTemplateKey is null)
            {
                return;
            }

            var template = TemplateStore.ResetTemplate(SelectedTemplateKey);
            Templates = TemplateStore.GetTemplates().ToList();
            SidebarItems = BuildSidebarItems();
            ValidationResult = null;
            await _editor.SetValue(template.DefaultCode);
            StatusText = "Template reset to default.";
        }

        private async Task ValidateCurrentTemplateAsync()
        {
            if (!CanEdit)
            {
                return;
            }

            var code = await _editor.GetValue();
            ValidationResult = RoslynCodeFactory.Validate(code);
            StatusText = ValidationResult.Success ? "Validation successful." : "Validation failed.";
        }

        private CodeTemplateViewModel? GetSelectedTemplate()
        {
            return SelectedTemplateKey is null
                ? null
                : Templates.FirstOrDefault(template => string.Equals(template.Key, SelectedTemplateKey, StringComparison.OrdinalIgnoreCase));
        }

        private string? GetInitialTemplateKey()
        {
            if (!string.IsNullOrWhiteSpace(QueryTemplateKey)
                && Templates.Any(template => string.Equals(template.Key, QueryTemplateKey, StringComparison.OrdinalIgnoreCase)))
            {
                return QueryTemplateKey;
            }

            return Templates.FirstOrDefault()?.Key;
        }

        private Task<SidebarDataProviderResult> DocSidebarDataProvider(SidebarDataProviderRequest request)
        {
            var docItems = new List<NavItem>
            {
                new NavItem { Id = "overview", Text = "Overview", IconName = IconName.InfoCircle, Href = "#overview" },
                new NavItem { Id = "categories", Text = "Categories", IconName = IconName.List, Href = "#categories" },
                new NavItem { Id = "structure", Text = "Structure", IconName = IconName.FileEarmarkCode, Href = "#structure" },
                new NavItem { Id = "base-methods", Text = "Base Methods", IconName = IconName.Hammer, Href = "#base-methods" },
                new NavItem { Id = "calculation", Text = "Calculation", IconName = IconName.Calculator, Href = "#calculation" },
                new NavItem { Id = "adjustment", Text = "Adjustment", IconName = IconName.Sliders, Href = "#adjustment" },
                new NavItem { Id = "distribution", Text = "Distribution", IconName = IconName.BoxSeam, Href = "#distribution" },
                new NavItem { Id = "validation", Text = "Validation", IconName = IconName.CheckCircle, Href = "#validation" },
                new NavItem { Id = "best-practices", Text = "Best Practices", IconName = IconName.Star, Href = "#best-practices" }
            };

            return Task.FromResult(request.ApplyTo(docItems));
        }
    }
}