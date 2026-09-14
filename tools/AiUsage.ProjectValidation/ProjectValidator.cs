using System.Text.RegularExpressions;

namespace AiUsage.ProjectValidation;

public sealed record Diagnostic(string File, string Task, string Code, string Message);

public static class ProjectValidator
{
    private sealed record Block(string Id, string File, string Text, Dictionary<string, string> Fields);
    private static readonly string[] BacklogStates = ["idea", "research-needed", "blocked", "ready", "selected", "in-progress", "paused", "review", "done", "dropped"];
    private static readonly string[] TaskStates = ["pending", "ready", "in-progress", "blocked", "done", "dropped"];
    private static readonly string[] DocStates = ["draft", "approved", "implementing", "implemented", "superseded"];
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(2);
    private static MatchCollection Matches(string text, string pattern) => Regex.Matches(text, pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant, MatchTimeout);
    private static string Value(Block block, string name) => block.Fields.GetValueOrDefault(name, "");
    private static string[] List(string value) => value.Trim().Trim('[', ']').Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim('"', '\'')).ToArray();
    private static string WithoutFences(string text) => Regex.Replace(text, @"(?ms)^```[^\n]*\n.*?^```[ \t]*$", "", RegexOptions.CultureInvariant, MatchTimeout);

    public static IReadOnlyList<Diagnostic> Validate(string root)
    {
        root = Path.GetFullPath(root);
        var errors = new List<Diagnostic>();
        var documents = new Dictionary<string, string>(StringComparer.Ordinal);
        void Error(string file, string task, string code, string message) => errors.Add(new(file, task, code, message));
        void Walk(string directory)
        {
            if (!Directory.Exists(directory)) return;
            if (!SafePath(root, Path.GetRelativePath(root, directory)))
            { Error(Path.GetRelativePath(root, directory).Replace('\\', '/'), "", "UNSAFE_PATH", "Reparse document directory or ancestor is not read."); return; }
            foreach (var path in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                { Error(relative, "", "UNSAFE_PATH", "Reparse document entry is not read."); continue; }
                if (relative.Equals("docs/archive", StringComparison.OrdinalIgnoreCase)) continue;
                if ((attributes & FileAttributes.Directory) != 0) Walk(path);
                else if (Path.GetExtension(path) is ".md" or ".txt" or ".ts" or ".yml" or ".toml") documents[relative] = File.ReadAllText(path).Replace("\r\n", "\n");
            }
        }
        Walk(Path.Combine(root, "docs"));
        Walk(Path.Combine(root, ".agents/skills"));
        foreach (var relative in new[] { "AGENTS.md", "CLAUDE.md", "README.md", "CONTRIBUTING.md", "SECURITY.md", ".github/copilot-instructions.md", ".omp/AGENTS.md" })
        {
            var path = Path.Combine(root, relative);
            if (!File.Exists(path)) continue;
            if (!SafePath(root, relative)) { Error(relative, "", "UNSAFE_PATH", "Reparse authored file is not read."); continue; }
            documents[relative] = File.ReadAllText(path).Replace("\r\n", "\n");
        }
        foreach (var required in new[] { "docs/product/goals.md", "docs/backlog.md" })
            if (!documents.ContainsKey(required)) Error(required, "", "MISSING_REFERENCE", "Required canonical document is missing.");
        foreach (var (file, text) in documents)
        {
            if (file.EndsWith(".md", StringComparison.Ordinal)) CheckLinks(root, file, text, Error);
        }
        CheckSkills(documents, Error);
        var goals = Parse(documents.GetValueOrDefault("docs/product/goals.md", ""), "docs/product/goals.md", "##", "G", 3, Error);
        var items = Parse(documents.GetValueOrDefault("docs/backlog.md", ""), "docs/backlog.md", "##", "AIU", 3, Error);
        foreach (var goal in goals) CheckFields(goal, Value(goal, "status") == "idea" ? ["status", "scope", "outcome"] : ["status", "scope", "outcome", "success"], BacklogStates, Error);
        foreach (var item in items)
        {
            CheckFields(item, ["goal", "status", "depends_on", "trigger", "outcome"], BacklogStates, Error);
            CheckEvidence(root, item, Error);
            var goal = goals.FirstOrDefault(g => g.Id == Value(item, "goal"));
            if (goal is null) Error(item.File, item.Id, "MISSING_GOAL", "Backlog goal does not exist.");
            else if (!List(Value(goal, "scope")).Contains(item.Id)) Error(goal.File, goal.Id, "GOAL_SCOPE", "Goal scope omits one of its backlog items.");
        }
        foreach (var goal in goals)
            foreach (var id in List(Value(goal, "scope")))
                if (!items.Any(i => i.Id == id && Value(i, "goal") == goal.Id)) Error(goal.File, goal.Id, "MISSING_REFERENCE", "Goal scope names a missing or differently owned item.");
        CheckDependencies(items, Error);
        var goalMeta = Metadata(documents.GetValueOrDefault("docs/product/goals.md", ""));
        if (!goalMeta.TryGetValue("active_goal", out var active) || !goals.Any(g => g.Id == active)) Error("docs/product/goals.md", "", "MISSING_GOAL", "Active goal does not exist.");
        foreach (var (file, text) in documents.Where(p => p.Key.EndsWith("/spec.md", StringComparison.Ordinal)))
        {
            var metadata = Metadata(text);
            var id = metadata.GetValueOrDefault("id", "");
            var spec = new Block(id, file, text, metadata);
            CheckFields(spec, ["id", "type", "status", "goal", "scope_version", "approval_basis"], DocStates, Error);
            if (!Regex.IsMatch(id, @"^AIU-\d{3}$")) Error(file, id, "INVALID_ID", "Specification ID must use AIU-NNN.");
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item is null) Error(file, id, "MISSING_REFERENCE", "Specification has no backlog item.");
            else
            {
                if (Value(spec, "goal") != Value(item, "goal")) Error(file, id, "MISSING_GOAL", "Specification and backlog goal differ.");
                if (Value(spec, "status") == "implemented" && Value(item, "status") != "done" || Value(item, "status") == "done" && Value(spec, "status") != "implemented") Error(file, id, "LIFECYCLE_STATUS", "Implemented specification and completed backlog state must agree.");
            }
            if (!int.TryParse(Value(spec, "scope_version"), out var scopeVersion) || scopeVersion < 1) Error(file, id, "REQUIRED_METADATA", "Positive scope_version required.");
            var ac = Matches(WithoutFences(text), @"^- (AC-[^: ]+):").Select(m => m.Groups[1].Value).ToArray();
            foreach (var criterion in ac)
                if (!Regex.IsMatch(criterion, @"^AC-\d{2}$")) Error(file, criterion, "INVALID_ID", "Acceptance ID must use AC-NN.");
            if (ac.Length == 0) Error(file, id, "AC_REFERENCE", "Specification requires acceptance criteria.");
            if (ac.Distinct().Count() != ac.Length) Error(file, id, "DUPLICATE_ID", "Duplicate acceptance criterion.");
            var folder = file[..file.LastIndexOf('/')];
            var designFile = folder + "/design.md";
            if (documents.TryGetValue(designFile, out var design))
            {
                var designBlock = new Block(id, designFile, design, Metadata(design));
                CheckFields(designBlock, ["id", "type", "status", "goal", "scope_version"], DocStates, Error);
                if (Value(designBlock, "id") != id || Value(designBlock, "goal") != Value(spec, "goal")) Error(designFile, id, "MISSING_REFERENCE", "Design ownership differs from specification.");
            }
            var taskFile = folder + "/tasks.md";
            if (!documents.TryGetValue(taskFile, out var taskText)) continue;
            var taskMeta = Metadata(taskText);
            if (taskMeta.GetValueOrDefault("id") != id || taskMeta.GetValueOrDefault("schema_version") != "1") Error(taskFile, id, "REQUIRED_METADATA", "Task metadata must name the feature and schema version 1.");
            var tasks = Parse(taskText, taskFile, "###", "T", 2, Error);
            CheckDependencies(tasks, Error);
            foreach (var task in tasks)
            {
                CheckFields(task, ["status", "depends_on", "acceptance", "evidence"], TaskStates, Error);
                if (Value(task, "parallel") == "true") CheckFields(task, ["ownership", "writes", "shared", "isolation", "agent"], TaskStates, Error);
                if (!new[] { "true", "false" }.Contains(task.Fields.GetValueOrDefault("parallel", "false")) || !new[] { "required", "none" }.Contains(task.Fields.GetValueOrDefault("isolation", "none"))) Error(taskFile, task.Id, "INVALID_STATUS", "Invalid parallel or isolation value.");
                var writes = List(Value(task, "writes"));
                var shared = List(Value(task, "shared"));
                foreach (var path in writes.Concat(shared)) if (!SafePath(root, path)) Error(taskFile, task.Id, "UNSAFE_PATH", "Ownership path is unsafe or traverses a reparse entry.");
                if (task.Fields.GetValueOrDefault("agent", "primary") != "primary" && writes.Length > 0)
                {
                    if (Value(task, "isolation") != "required") Error(taskFile, task.Id, "WORKER_ISOLATION", "Write worker must require isolation.");
                    if (shared.Length > 0 || writes.Any(PrimaryPath)) Error(taskFile, task.Id, "PRIMARY_SHARED", "Shared state belongs to the primary.");
                }
                var refs = List(Value(task, "acceptance"));
                if (refs.Length == 0 || refs.Any(r => !ac.Contains(r))) Error(taskFile, task.Id, "AC_REFERENCE", "Task acceptance reference is missing from its specification.");
                CheckEvidence(root, task, Error);
                if (Value(task, "status") == "done")
                {
                    var evidence = Value(task, "evidence");
                    if (task.Fields.GetValueOrDefault("agent", "primary") != "primary" && writes.Length > 0 && !evidence.Contains("integrated", StringComparison.OrdinalIgnoreCase)) Error(taskFile, task.Id, "DONE_WITHOUT_INTEGRATION", "Write worker completion requires recorded primary integration.");
                    if (List(Value(task, "depends_on")).Any(d => !tasks.Any(t => t.Id == d && Value(t, "status") == "done"))) Error(taskFile, task.Id, "MISSING_DEPENDENCY", "Done task has an unfinished dependency.");
                }
            }
            for (var a = 0; a < tasks.Count; a++)
                for (var b = a + 1; b < tasks.Count; b++)
                {
                    var left = tasks[a]; var right = tasks[b];
                    if (Value(left, "parallel") != "true" || Value(right, "parallel") != "true" || new[] { "done", "dropped" }.Contains(Value(left, "status")) || new[] { "done", "dropped" }.Contains(Value(right, "status")) || Depends(left.Id, right.Id, tasks, []) || Depends(right.Id, left.Id, tasks, [])) continue;
                    if (Value(left, "ownership") == Value(right, "ownership") || List(Value(left, "writes")).Any(l => List(Value(right, "writes")).Any(r => Overlap(l, r)))) Error(taskFile, right.Id, "PARALLEL_OVERLAP", $"Concurrent ownership overlaps {left.Id}; serialize the tasks.");
                }

        }
        return errors.OrderBy(e => e.File, StringComparer.Ordinal).ThenBy(e => e.Task, StringComparer.Ordinal).ThenBy(e => e.Code, StringComparer.Ordinal).ToArray();
    }

    private static void CheckEvidence(string root, Block block, Action<string, string, string, string> error)
    {
        var paths = Matches(Value(block, "evidence"), @"[^\s;,`()\[\]]*[/\\][^\s;,`()\[\]]+\.[a-zA-Z0-9]+[^\s;,`()\[\]]*")
            .Select(m => m.Value)
            .Where(path => !Uri.TryCreate(path, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            .ToArray();
        foreach (var path in paths)
            if (!SafePath(root, path)) error(block.File, block.Id, "UNSAFE_PATH", "Evidence path is unsafe or traverses a reparse entry.");
        if (Value(block, "status") == "done" && !paths.Any(path => SafePath(root, path) && File.Exists(Path.Combine(root, path))))
            error(block.File, block.Id, "DONE_WITHOUT_EVIDENCE", "Completion requires an existing safe evidence artifact.");
    }

    private static void CheckSkills(Dictionary<string, string> documents, Action<string, string, string, string> error)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (file, text) in documents.Where(p =>
            p.Key.StartsWith(".agents/skills/", StringComparison.Ordinal) &&
            p.Key.EndsWith("/SKILL.md", StringComparison.Ordinal)))
        {
            var metadata = Metadata(text);
            var name = metadata.GetValueOrDefault("name", "");
            if (name.Length is < 1 or > 64 || !Regex.IsMatch(name, @"^[a-z0-9]+(?:-[a-z0-9]+)*$") ||
                string.IsNullOrWhiteSpace(metadata.GetValueOrDefault("description")))
                error(file, "", "SKILL_METADATA", "Skill requires a kebab-case name and nonempty description in frontmatter.");
            if (!names.Add(name))
                error(file, name, "DUPLICATE_SKILL", "Shared skill names must be unique.");
            var expected = $".agents/skills/{name}/SKILL.md";
            if (file != expected) error(file, name, "SKILL_PATH", "Skill name must match its direct repository skill directory.");
        }
    }

    private static Dictionary<string, string> Metadata(string text)
    {
        if (!text.StartsWith("---\n", StringComparison.Ordinal)) return [];
        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        return end < 0 ? [] : Matches(text[4..end], @"^([a-z_][a-z_0-9]*):[ \t]*(.*)$").GroupBy(m => m.Groups[1].Value).ToDictionary(g => g.Key, g => g.Last().Groups[2].Value.Trim());
    }
    private static List<Block> Parse(string text, string file, string level, string prefix, int digits, Action<string, string, string, string> error)
    {
        text = WithoutFences(text);
        var matches = Matches(text, $@"^{level} ({prefix}-[^ ]+) - (.+)$");
        var blocks = new List<Block>();
        for (var i = 0; i < matches.Count; i++)
        {
            var id = matches[i].Groups[1].Value;
            var body = text[(matches[i].Index + matches[i].Length)..(i + 1 < matches.Count ? matches[i + 1].Index : text.Length)];
            var fields = Matches(body, @"^- ([a-z_]+): (.*)$").GroupBy(m => m.Groups[1].Value).ToDictionary(g => g.Key, g => g.First().Groups[2].Value.Trim());
            if (!Regex.IsMatch(id, $@"^{prefix}-\d{{{digits}}}$")) error(file, id, "INVALID_ID", $"ID must use {prefix} and {digits} digits.");
            if (blocks.Any(b => b.Id == id)) error(file, id, "DUPLICATE_ID", "Duplicate ID in document namespace.");
            blocks.Add(new(id, file, body, fields));
        }
        return blocks;
    }
    private static void CheckFields(Block block, string[] required, string[] states, Action<string, string, string, string> error)
    {
        foreach (var key in required) if (string.IsNullOrWhiteSpace(Value(block, key))) error(block.File, block.Id, "REQUIRED_METADATA", $"Missing {key}.");
        if (!states.Contains(Value(block, "status"))) error(block.File, block.Id, "INVALID_STATUS", "Status is not permitted for this document.");
    }
    private static void CheckDependencies(List<Block> blocks, Action<string, string, string, string> error)
    {
        foreach (var block in blocks)
            foreach (var id in List(Value(block, "depends_on")))
            {
                if (!blocks.Any(b => b.Id == id)) error(block.File, block.Id, "MISSING_DEPENDENCY", "Dependency does not exist in this namespace.");
                else if (id == block.Id || Depends(id, block.Id, blocks, [])) error(block.File, block.Id, "DEPENDENCY_CYCLE", "Dependency graph contains a cycle.");
            }
    }
    private static bool Depends(string from, string target, List<Block> blocks, HashSet<string> seen)
    {
        if (!seen.Add(from)) return false;
        var block = blocks.FirstOrDefault(b => b.Id == from);
        return block is not null && List(Value(block, "depends_on")).Any(id => id == target || Depends(id, target, blocks, seen));
    }
    private static bool PrimaryPath(string path)
    {
        path = path.Replace('\\', '/').ToLowerInvariant();
        return path.StartsWith(".omp/") || path.StartsWith(".agents/") || path.StartsWith(".github/") || path is "agents.md" or "claude.md" or "contributing.md" or "security.md" or "docs/constitution.md" or "docs/backlog.md" or "docs/product/goals.md" || path.EndsWith("/tasks.md");
    }
    private static bool Overlap(string a, string b)
    {
        a = a.Replace('\\', '/').ToLowerInvariant(); b = b.Replace('\\', '/').ToLowerInvariant();
        var left = a.EndsWith("/**") ? a[..^3] : a; var right = b.EndsWith("/**") ? b[..^3] : b;
        if (left.IndexOfAny(['*', '?', '[', '{']) >= 0 || right.IndexOfAny(['*', '?', '[', '{']) >= 0) return true;
        return left == right || left.StartsWith(right + "/", StringComparison.Ordinal) || right.StartsWith(left + "/", StringComparison.Ordinal);
    }
    private static bool SafePath(string root, string value)
    {
        var path = value.Replace('\\', '/');
        if (path.Length == 0 || path.StartsWith('/') || path.StartsWith('~') || path.Contains(':') || path.Any(c => char.IsControl(c) || c is '<' or '>' or '|' or '"')) return false;
        var current = root;
        var parts = path.Split('/');
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            if (part.Length == 0 || part is "." or ".." || part.Equals(".git", StringComparison.OrdinalIgnoreCase) || part.EndsWith('.') || part.EndsWith(' ') || Regex.IsMatch(part, @"^(?i:con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)")) return false;
            if (part.IndexOfAny(['*', '?', '[', '{']) >= 0) return part == "**" && index == parts.Length - 1;
            current = Path.Combine(current, part);
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
        }
        return true;
    }
    private static void CheckLinks(string root, string file, string text, Action<string, string, string, string> error)
    {
        text = WithoutFences(text);
        var links = Matches(text, @"\[[^\]\n]*\]\(([^)\s]+)(?:\s+[^)]*)?\)|^\[[^\]]+\]:\s*(\S+)");
        foreach (Match link in links)
        {
            var destination = (link.Groups[1].Success ? link.Groups[1].Value : link.Groups[2].Value).Trim('<', '>');
            if (Uri.TryCreate(destination, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto") continue;
            var target = Uri.UnescapeDataString(destination.Split('#')[0]);
            var resolved = Path.GetFullPath(Path.Combine(root, Path.GetDirectoryName(file)!, target.Length == 0 ? Path.GetFileName(file) : target));
            var relative = Path.GetRelativePath(root, resolved).Replace('\\', '/');
            if (!SafePath(root, relative) || !File.Exists(resolved) && !Directory.Exists(resolved)) error(file, "", "BROKEN_LINK", "Local Markdown link is missing or escapes the repository.");
        }
    }
}
