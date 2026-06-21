namespace ArchiveFlow.Application.Nodes.Definitions;

/// <summary>
/// Provides the built-in node definitions used by ArchiveFlow Studio.
/// </summary>
public static class BuiltInNodeDefinitions
{
    public static IReadOnlyList<NodeDefinition> CreateAll()
    {
        var definitions = new List<NodeDefinition>();

        AddSources(definitions);
        AddQueryFilters(definitions);
        AddSearch(definitions);
        AddLogic(definitions);
        AddMetadataActions(definitions);
        AddFileActions(definitions);
        AddCreateAndTemplate(definitions);
        AddRelationships(definitions);
        AddIndexingAndExtraction(definitions);
        AddOutputs(definitions);

        return definitions;
    }

    private static void AddSources(List<NodeDefinition> definitions)
    {
        definitions.Add(Source("source.all_files", "All Files", "Loads every file record from the archive database."));
        definitions.Add(Source("source.folder_source", "Folder Source", "Uses a selected folder as the source of a file workflow.",
            new[]
            {
                Text("folderPath", "Folder Path", "")
            }));
        definitions.Add(Source("source.recent_imports", "Recent Imports", "Loads recently imported files."));
        definitions.Add(Source("source.selected_files", "Selected Files", "Uses the files selected in the result table."));
        definitions.Add(Source("source.unorganized_files", "Unorganized Files", "Loads files that need organization."));
        definitions.Add(Source("source.missing_metadata", "Missing Metadata Files", "Loads files with missing required metadata."));
        definitions.Add(Source("source.duplicate_files", "Duplicate Files", "Loads files that appear to be duplicates."));
    }

    private static void AddQueryFilters(List<NodeDefinition> definitions)
    {
        definitions.Add(Filter("filter.file_type", "File Type Filter", "Filters files by broad file type.",
            new[]
            {
                Dropdown("fileType", "File Type", "Document", "Document", "Image", "Video", "Audio", "Code", "3D Model", "Archive", "Other")
            }));

        definitions.Add(Filter("filter.extension", "Extension Filter", "Filters files by extension.",
            new[]
            {
                Text("extension", "Extension", ".pdf")
            }));

        definitions.Add(Filter("filter.date_range", "Date Range Filter", "Filters files by imported or modified date.",
            new[]
            {
                Date("startDate", "Start Date", ""),
                Date("endDate", "End Date", "")
            }));

        definitions.Add(Filter("filter.size", "Size Filter", "Filters files by size range.",
            new[]
            {
                Number("minSizeMb", "Minimum Size MB", "0"),
                Number("maxSizeMb", "Maximum Size MB", "100")
            }));

        definitions.Add(Filter("filter.path_contains", "Path Contains Filter", "Filters files whose path contains specific text.",
            new[]
            {
                Text("pathText", "Path Contains", "")
            }));

        definitions.Add(Filter("filter.tag", "Tag Filter", "Filters files by tag.",
            new[]
            {
                Text("tag", "Tag", "")
            }));

        definitions.Add(Filter("filter.subject", "Subject Filter", "Filters files by subject.",
            new[]
            {
                Text("subject", "Subject", "")
            }));

        definitions.Add(Filter("filter.metadata_field", "Metadata Field Filter", "Filters files by metadata field expression.",
            new[]
            {
                Text("fieldName", "Field Name", "subject"),
                Dropdown("operator", "Operator", "contains", "equals", "contains", "starts with", "ends with", "is empty", "is not empty"),
                Text("value", "Value", "")
            }));

        definitions.Add(Filter("filter.status", "Status Filter", "Filters files by archive or processing status.",
            new[]
            {
                Dropdown("status", "Status", "New", "New", "Scanned", "Archived", "Incomplete", "Duplicate", "Missing")
            }));
    }

    private static void AddSearch(List<NodeDefinition> definitions)
    {
        definitions.Add(Search("search.keyword", "Keyword Search", "Searches filenames and common metadata fields.",
            new[]
            {
                Text("query", "Keyword", "")
            }));

        definitions.Add(Search("search.full_text", "Full-text Search", "Searches extracted text content.",
            new[]
            {
                Text("query", "Full-text Query", "")
            }));

        definitions.Add(Search("search.boolean", "Boolean Search", "Searches using a Boolean expression.",
            new[]
            {
                Text("expression", "Boolean Expression", "AI AND metadata")
            }));

        definitions.Add(Search("search.filename", "Filename Search", "Searches by filename.",
            new[]
            {
                Text("filename", "Filename Contains", "")
            }));

        definitions.Add(Search("search.content", "Content Search", "Searches indexed content text.",
            new[]
            {
                Text("contentQuery", "Content Query", "")
            }));
    }

    private static void AddLogic(List<NodeDefinition> definitions)
    {
        definitions.Add(Logic("logic.and", "AND", "Keeps files that match all connected inputs."));
        definitions.Add(Logic("logic.or", "OR", "Keeps files that match any connected input."));
        definitions.Add(Logic("logic.not", "NOT", "Excludes files from the input set."));
        definitions.Add(Logic("logic.sort_by", "Sort By", "Sorts the current file set.",
            new[]
            {
                Dropdown("field", "Sort Field", "Imported Date", "Imported Date", "Modified Date", "Filename", "Size", "Status"),
                Dropdown("direction", "Direction", "Descending", "Ascending", "Descending")
            }));
        definitions.Add(Logic("logic.limit", "Limit", "Limits the number of output files.",
            new[]
            {
                Number("count", "Count", "100")
            }));
        definitions.Add(Logic("logic.group_by", "Group By", "Groups files by a selected field.",
            new[]
            {
                Dropdown("field", "Group Field", "Extension", "Extension", "Subject", "Tag", "Project", "Status", "Year")
            }));
        definitions.Add(Logic("logic.union", "Union", "Combines multiple file sets."));
        definitions.Add(Logic("logic.intersection", "Intersection", "Keeps files present in all input sets."));
        definitions.Add(Logic("logic.difference", "Difference", "Subtracts one file set from another."));
    }

    private static void AddMetadataActions(List<NodeDefinition> definitions)
    {
        definitions.Add(MetadataAction("metadata.add_tag", "Add Tag", "Adds a tag to all input files.",
            new[]
            {
                Text("tag", "Tag", "AI")
            }));

        definitions.Add(MetadataAction("metadata.remove_tag", "Remove Tag", "Removes a tag from all input files.",
            new[]
            {
                Text("tag", "Tag", "")
            }));

        definitions.Add(MetadataAction("metadata.set_subject", "Set Subject", "Sets the subject metadata field.",
            new[]
            {
                Text("subject", "Subject", "")
            }));

        definitions.Add(MetadataAction("metadata.set_project", "Set Project", "Sets the project metadata field.",
            new[]
            {
                Text("project", "Project", "")
            }));

        definitions.Add(MetadataAction("metadata.set_status", "Set Status", "Sets the processing or archive status.",
            new[]
            {
                Dropdown("status", "Status", "Archived", "New", "Scanned", "Archived", "Incomplete", "Duplicate")
            }));

        definitions.Add(MetadataAction("metadata.set_reading_status", "Set Reading Status", "Sets the reading status.",
            new[]
            {
                Dropdown("readingStatus", "Reading Status", "To Read", "To Read", "Reading", "Read", "Reference", "Not Applicable")
            }));

        definitions.Add(MetadataAction("metadata.set_importance", "Set Importance", "Sets personal importance level.",
            new[]
            {
                Dropdown("importance", "Importance", "Normal", "Low", "Normal", "High", "Critical")
            }));

        definitions.Add(MetadataAction("metadata.generate_archive_id", "Generate Archive ID", "Generates or refreshes archive identifiers.",
            new[]
            {
                Text("pattern", "Archive ID Pattern", "AF-{yyyyMMdd}-{sequence}")
            }));

        definitions.Add(MetadataAction("metadata.validate_metadata", "Validate Metadata", "Checks metadata completeness and validation rules."));
    }

    private static void AddFileActions(List<NodeDefinition> definitions)
    {
        definitions.Add(FileAction("file.copy_to_archive", "Copy to Archive", "Copies files into the managed archive area.",
            new[]
            {
                Text("targetFolder", "Target Folder", "")
            }));

        definitions.Add(FileAction("file.move_file", "Move File", "Moves physical files. Requires preview and apply.",
            new[]
            {
                Text("targetFolder", "Target Folder", "")
            }));

        definitions.Add(FileAction("file.rename_file", "Rename File", "Renames physical files using a pattern.",
            new[]
            {
                Text("pattern", "Rename Pattern", "{archiveId}-{filename}")
            }));

        definitions.Add(FileAction("file.open_file", "Open File", "Opens the selected file using the system default app."));
        definitions.Add(FileAction("file.reveal", "Reveal in File Explorer", "Reveals the physical file location."));
    }

    private static void AddCreateAndTemplate(List<NodeDefinition> definitions)
    {
        definitions.Add(Create("create.markdown_note", "Create Markdown Note", "Creates a Markdown note with metadata.",
            new[]
            {
                Text("title", "Title", "Untitled Note"),
                Dropdown("template", "Template", "Basic Note", "Basic Note", "Research Note", "Reading Note", "Project Note")
            }));

        definitions.Add(Create("create.research_record", "Create Research Record", "Creates a research record with descriptive metadata."));
        definitions.Add(Create("create.project_folder", "Create Project Folder", "Creates a structured project folder."));
        definitions.Add(Create("create.collection", "Create Collection", "Creates a collection record."));
        definitions.Add(Create("create.metadata_template", "Create Metadata Template", "Creates a reusable metadata template."));
        definitions.Add(Create("create.reading_list", "Create Reading List", "Creates a reading list from input files."));
    }

    private static void AddRelationships(List<NodeDefinition> definitions)
    {
        definitions.Add(Relationship("relationship.create", "Create Relationship", "Creates a relationship between files.",
            new[]
            {
                Text("targetFileId", "Target File ID", ""),
                Dropdown("relationshipType", "Relationship Type", "relatedTo", "relatedTo", "hasNote", "hasSource", "hasExport", "isVersionOf", "references", "derivedFrom")
            }));

        definitions.Add(Relationship("relationship.find_related", "Find Related Files", "Finds files related to the input set."));
        definitions.Add(Relationship("relationship.same_project", "Find Same Project", "Finds files in the same project."));
        definitions.Add(Relationship("relationship.same_subject", "Find Same Subject", "Finds files with the same subject."));
        definitions.Add(Relationship("relationship.find_notes", "Find Notes", "Finds notes connected to files."));
        definitions.Add(Relationship("relationship.link_note", "Link Note", "Links a note to source files."));
        definitions.Add(Relationship("relationship.link_source", "Link Source", "Links a source file."));
        definitions.Add(Relationship("relationship.link_export", "Link Export", "Links an exported derivative."));
        definitions.Add(Relationship("relationship.build_graph", "Build Graph View", "Builds graph data from relationships."));
    }

    private static void AddIndexingAndExtraction(List<NodeDefinition> definitions)
    {
        definitions.Add(Indexing("index.hash", "Calculate Hash", "Calculates file hashes."));
        definitions.Add(Indexing("index.detect_duplicates", "Detect Duplicates", "Detects duplicate files by hash."));
        definitions.Add(Indexing("index.extract_text", "Extract Text", "Extracts text for indexing."));
        definitions.Add(Indexing("index.full_text", "Build Full-text Index", "Builds the local full-text search index."));
        definitions.Add(Indexing("index.thumbnail", "Generate Thumbnail", "Generates thumbnails for supported files."));
        definitions.Add(Indexing("index.pdf_metadata", "Extract PDF Metadata", "Extracts embedded PDF metadata."));
        definitions.Add(Indexing("index.ocr", "Run OCR", "Runs OCR for images and scanned documents."));
    }

    private static void AddOutputs(List<NodeDefinition> definitions)
    {
        definitions.Add(Output("output.result_table", "Result Table", "Displays workflow output as a table."));
        definitions.Add(Output("output.gallery", "Gallery View", "Displays image-like results as a gallery."));
        definitions.Add(Output("output.card", "Card View", "Displays results as metadata cards."));
        definitions.Add(Output("output.timeline", "Timeline View", "Displays results by date."));
        definitions.Add(Output("output.graph", "Graph View", "Displays relationships as a graph."));
        definitions.Add(OutputAction("output.export_csv", "Export CSV", "Exports results to CSV.",
            new[]
            {
                Text("fileName", "File Name", "archiveflow-results.csv")
            }));
        definitions.Add(OutputAction("output.export_json", "Export JSON", "Exports results to JSON.",
            new[]
            {
                Text("fileName", "File Name", "archiveflow-results.json")
            }));
        definitions.Add(OutputAction("output.export_dublin_core", "Export Dublin Core", "Exports metadata using Dublin Core."));
        definitions.Add(OutputAction("output.smart_collection", "Create Smart Collection", "Creates a saved smart collection."));
    }

    private static NodeDefinition Source(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return new NodeDefinition
        {
            NodeType = nodeType,
            DisplayName = name,
            Category = NodeCategory.Sources,
            Subcategory = "Sources",
            Description = description,
            Icon = "◎",
            AccentColor = "#4CAF50",
            IsPreviewOnly = true,
            IsActionNode = false,
            OutputPorts = new[] { Port("Files", NodePortDataType.FileSet) },
            Parameters = parameters ?? Array.Empty<NodeParameterDefinition>()
        };
    }

    private static NodeDefinition Filter(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return QueryLike(nodeType, name, NodeCategory.QueryFilters, "Query Filters", description, "◇", "#2196F3", parameters);
    }

    private static NodeDefinition Search(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return QueryLike(nodeType, name, NodeCategory.Search, "Search", description, "⌕", "#03A9F4", parameters);
    }

    private static NodeDefinition Logic(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return QueryLike(nodeType, name, NodeCategory.LogicAndSetOperations, "Logic & Set Operations", description, "◆", "#3F51B5", parameters);
    }

    private static NodeDefinition QueryLike(
        string nodeType,
        string name,
        NodeCategory category,
        string subcategory,
        string description,
        string icon,
        string color,
        IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return new NodeDefinition
        {
            NodeType = nodeType,
            DisplayName = name,
            Category = category,
            Subcategory = subcategory,
            Description = description,
            Icon = icon,
            AccentColor = color,
            IsPreviewOnly = true,
            IsActionNode = false,
            InputPorts = new[] { Port("Files", NodePortDataType.FileSet) },
            OutputPorts = new[] { Port("Files", NodePortDataType.FileSet) },
            Parameters = parameters ?? Array.Empty<NodeParameterDefinition>()
        };
    }

    private static NodeDefinition MetadataAction(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return ActionLike(nodeType, name, NodeCategory.MetadataActions, "Metadata Actions", description, "✎", "#FF9800", parameters);
    }

    private static NodeDefinition FileAction(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return ActionLike(nodeType, name, NodeCategory.FileActions, "File Actions", description, "▣", "#F44336", parameters);
    }

    private static NodeDefinition Create(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return ActionLike(nodeType, name, NodeCategory.CreateAndTemplate, "Create / Template", description, "＋", "#795548", parameters);
    }

    private static NodeDefinition Relationship(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return ActionLike(nodeType, name, NodeCategory.Relationships, "Relationships", description, "⛓", "#00BCD4", parameters);
    }

    private static NodeDefinition Indexing(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return ActionLike(nodeType, name, NodeCategory.IndexingAndExtraction, "Indexing & Extraction", description, "⚙", "#9E9E9E", parameters);
    }

    private static NodeDefinition ActionLike(
        string nodeType,
        string name,
        NodeCategory category,
        string subcategory,
        string description,
        string icon,
        string color,
        IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return new NodeDefinition
        {
            NodeType = nodeType,
            DisplayName = name,
            Category = category,
            Subcategory = subcategory,
            Description = description,
            Icon = icon,
            AccentColor = color,
            IsPreviewOnly = false,
            IsActionNode = true,
            InputPorts = new[] { Port("Files", NodePortDataType.FileSet) },
            OutputPorts = new[] { Port("Files", NodePortDataType.FileSet) },
            Parameters = parameters ?? Array.Empty<NodeParameterDefinition>()
        };
    }

    private static NodeDefinition Output(string nodeType, string name, string description)
    {
        return new NodeDefinition
        {
            NodeType = nodeType,
            DisplayName = name,
            Category = NodeCategory.Outputs,
            Subcategory = "Outputs",
            Description = description,
            Icon = "▤",
            AccentColor = "#9C27B0",
            IsPreviewOnly = true,
            IsActionNode = false,
            InputPorts = new[] { Port("Files", NodePortDataType.FileSet) }
        };
    }

    private static NodeDefinition OutputAction(string nodeType, string name, string description, IReadOnlyList<NodeParameterDefinition>? parameters = null)
    {
        return new NodeDefinition
        {
            NodeType = nodeType,
            DisplayName = name,
            Category = NodeCategory.Outputs,
            Subcategory = "Outputs",
            Description = description,
            Icon = "⇩",
            AccentColor = "#9C27B0",
            IsPreviewOnly = false,
            IsActionNode = true,
            InputPorts = new[] { Port("Files", NodePortDataType.FileSet) },
            Parameters = parameters ?? Array.Empty<NodeParameterDefinition>()
        };
    }

    private static NodePortDefinition Port(string name, NodePortDataType dataType)
    {
        return new NodePortDefinition
        {
            Name = name,
            DataType = dataType
        };
    }

    private static NodeParameterDefinition Text(string key, string name, string defaultValue)
    {
        return new NodeParameterDefinition
        {
            Key = key,
            DisplayName = name,
            ControlType = NodeParameterControlType.Text,
            DefaultValue = defaultValue
        };
    }

    private static NodeParameterDefinition Number(string key, string name, string defaultValue)
    {
        return new NodeParameterDefinition
        {
            Key = key,
            DisplayName = name,
            ControlType = NodeParameterControlType.Number,
            DefaultValue = defaultValue
        };
    }

    private static NodeParameterDefinition Date(string key, string name, string defaultValue)
    {
        return new NodeParameterDefinition
        {
            Key = key,
            DisplayName = name,
            ControlType = NodeParameterControlType.Date,
            DefaultValue = defaultValue
        };
    }

    private static NodeParameterDefinition Dropdown(string key, string name, string defaultValue, params string[] options)
    {
        return new NodeParameterDefinition
        {
            Key = key,
            DisplayName = name,
            ControlType = NodeParameterControlType.Dropdown,
            DefaultValue = defaultValue,
            Options = options
        };
    }
}