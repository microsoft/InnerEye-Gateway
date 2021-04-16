using Dicom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AnonFunc = System.Func<Dicom.DicomDataset, System.Collections.Generic.List<DICOMAnonymizer.TagOrIndex>, Dicom.DicomItem, Dicom.DicomItem>;

namespace DICOMAnonymizer
{
    public class AnonymizeEngine
    {
        /// <summary>
        /// The engine operates in 3 different modes:
        /// Blank: Introduces a new empty dataset to TagHandlers. 
        ///        TagHandlers should explicitly add needed attributes to the empty dataset.
        ///        This method accommodates explicit whitelist scenarios.
        /// Clone: Clones the input to a new object, which is then provided to the TagHandler.
        /// InPlace: Anonymizes the input on the spot.
        /// </summary>
        public enum Mode { inplace, blank, clone }

        private static readonly char[] _parenthesis = new[] { '(', ')' };
        private readonly Dictionary<Regex, AnonFunc> _regexFuncs = new Dictionary<Regex, AnonFunc>();
        private readonly Dictionary<DicomTag, AnonFunc> _tagFuncs = new Dictionary<DicomTag, AnonFunc>();
        private readonly List<ITagHandler> _tagHandlers = new List<ITagHandler>();
        private List<ITagHandler> _postprocesses { get; set; } = new List<ITagHandler>();

        // TODO: A way to make descriptions optional during testing
        private readonly bool _testingmode = false;
        private AnonymizeEngine(Mode m, bool testing)
        {
            _mode = m;
            _testingmode = testing;
        }

        public AnonymizeEngine(Mode m)
        {
            _mode = m;
        }

        private void CheckAnonymizationAttributesExist(AnonFunc f)
        {
            if (_testingmode)
            {
                return;
            }

            if (f.Method.GetCustomAttributes(typeof(DescriptionAttribute), false).Length != 1)
            {
                throw new FormatException(string.Format("{0}:{1} Description attribute is missing", f.Target.GetType().FullName, f.Method.Name));
            }
        }

        private void CheckExamplesExist(ITagHandler th, AnonFunc f)
        {
            if (_testingmode)
            {
                return;
            }

            var exampMethod = th.GetType().GetMethod(f.Method.Name + "Examples");
            if (exampMethod == null)
            {
                throw new FormatException(string.Format("{0}:{1} Examples are missing.", f.Target.GetType().FullName, f.Method.Name));
            }
            if (!exampMethod.IsStatic)
            {
                throw new FormatException(string.Format("{0}:{1} should be static.", f.Target.GetType().FullName, f.Method.Name));
            }
        }

        /// <summary>
        /// Register all actions of a TagHandler. If a tag is already handled, we throw an error.
        /// </summary>
        /// <param name="th">The tag handler from which tag actions will be consumed</param>
        public void RegisterHandler(ITagHandler th)
        {
            _tagHandlers.Add(th);
            _postprocesses.Add(th);

            // TODO: check that handlers respect VR
            th.GetTagFuncs()?.ToList().ForEach(x =>
            {
                CheckAnonymizationAttributesExist(x.Value);
                CheckExamplesExist(th, x.Value);
                _tagFuncs.Add(x.Key, x.Value);
            });
            th.GetRegexFuncs()?.ToList().ForEach(x =>
            {
                CheckAnonymizationAttributesExist(x.Value);
                CheckExamplesExist(th, x.Value);
                _regexFuncs.Add(x.Key, x.Value);
            });
        }

        /// <summary>
        /// Register all actions of a TagHandler. Overwrites duplicates in favor of new actions. The actions can have side-effects.
        /// </summary>
        /// <param name="th">The tag handler from which tag actions will be consumed</param>
        public List<string> ForceRegisterHandler(ITagHandler th)
        {
            _tagHandlers.Add(th);
            _postprocesses.Add(th);

            var report = new List<string>();
            th.GetTagFuncs()?.ToList().ForEach(x =>
            {
                CheckAnonymizationAttributesExist(x.Value);
                CheckExamplesExist(th, x.Value);

                if (_tagFuncs.ContainsKey(x.Key))
                {
                    report.Add(TagName(x.Key));
                }

                _tagFuncs[x.Key] = x.Value;
            });
            th.GetRegexFuncs()?.ToList().ForEach(x =>
            {
                CheckAnonymizationAttributesExist(x.Value);
                CheckExamplesExist(th, x.Value);

                if (_regexFuncs.FirstOrDefault(pair => pair.Key.ToString().Equals(x.Key.ToString())).Key != null)
                {
                    report.Add(x.Key.ToString());
                }

                _regexFuncs[x.Key] = x.Value;
            });
            return report;
        }

        private readonly Mode _mode;

        private DicomDataset ModeSwitch(DicomDataset ds, Mode m)
        {
            switch (m)
            {
                case Mode.inplace:
                    return ds;
                case Mode.blank:
                    return new DicomDataset();
                default:
                    throw new InvalidOperationException();
            }
        }

        private DicomDataset DoAnonymization(DicomDataset oldds, Stack<TagOrIndex> stack, Mode m)
        {
            foreach (var th in _postprocesses)
            {
                th.NextDataset();
            }

            var newds = ModeSwitch(oldds, m);
            var arr = oldds.ToList();

            for (int i = 0; i < arr.Count; i++)
            {
                var item = arr[i];

                // Use DFS to reach leaves first and then decide if we want
                // to keep the sequence. This is not unoptimized code. The
                // user might still need to visit the children even if it
                // deletes the Seq. Eventually we should support Enter
                // and Exit methods for Seqs.
                if (item is DicomSequence)
                {
                    DicomSequence nseq = new DicomSequence(item.Tag);
                    stack.Push(new TagOrIndex(item.Tag));
                    // Visit sequence's children
                    foreach (var tuple in (item as DicomSequence).Items.ToList().Select((value, j) => new { j, value }))
                    {
                        var seqds = tuple.value;
                        var index = tuple.j;

                        stack.Push(new TagOrIndex(index));

                        var n = DoAnonymization(seqds, stack, m);
                        // The only reason we get an empty DicomDataset is because
                        // we deleted its tags during recursion.
                        if (n.Count() > 0) // So we skip it
                        {
                            nseq.Items.Add(n);
                        }
                        if (seqds.Count() == 0) // AND we remove it from the original seq (this is for inplace mode)
                        {
                            (item as DicomSequence).Items.Remove(seqds);
                        }

                        stack.Pop();
                    }
                    item = nseq;
                    stack.Pop();
                }

                AnonFunc handler = null;

                // First we try to see if there is a string tag handler
                if (_tagFuncs.ContainsKey(item.Tag))
                {
                    handler = _tagFuncs[item.Tag];
                }
                else // If no string tag exists, check for regex
                {
                    var tag = item.Tag.ToString().Trim(_parenthesis);
                    // Check against regex is rather slow as we need to visit all of them linearly
                    var regAct = _regexFuncs.FirstOrDefault(pair => pair.Key.IsMatch(tag));
                    if (regAct.Key != null)
                    {
                        handler = regAct.Value;
                    }
                }

                // No registered handler found
                if (handler == null)
                {
                    continue;
                }

                // Item handler found
                if (item is DicomElement || item is DicomSequence || item is DicomFragmentSequence)
                {
                    // We don't include the item's tag in the path.
                    var r = handler(oldds, stack.Reverse().ToList(), item);
                    if (r != null)
                    {
                        newds.AddOrUpdate(r);
                    }
                    else
                    {
                        newds.Remove(item.Tag);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Can't handle type: {item.GetType()}");
                }

                // TODO: Log which is the current iterated tag, and which function was invoked
            }

            foreach (var th in _postprocesses)
            {
                th.Postprocess(newds);
            }
            return newds;
        }

        /// <summary>
        /// Anonymize a dicom dataset
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public DicomDataset Anonymize(DicomDataset dataset)
        {
            Mode m = _mode;
            if (m == Mode.clone)
            {
                // TODO: Anonymizing DicomElement needs deep copy
                // TODO: oldds and newds are pointless as newds' elements are pointers to oldds'. Any change affects both objects.
                dataset = dataset.Clone();
                m = Mode.inplace;
            }

            return DoAnonymization(dataset, new Stack<TagOrIndex>(), m);
        }

        /// <summary>Anonymizes a Dicom file (dataset + metadata)</summary>
        /// <param name="file">The file containing the dataset to be altered</param>
        public DicomFile Anonymize(DicomFile file)
        {
            var transferSyntax = file.FileMetaInfo.Contains(DicomTag.TransferSyntaxUID) ? file.FileMetaInfo.TransferSyntax : null;
            Mode m = _mode;
            if (m == Mode.clone)
            {
                file = file.Clone();
                m = Mode.inplace;
            }

            var ds = DoAnonymization(file.Dataset, new Stack<TagOrIndex>(), m);
            if (file.FileMetaInfo != null)
            {
                file = new DicomFile(ds);
                file.FileMetaInfo.ImplementationVersionName = "";
                file.FileMetaInfo.SourceApplicationEntityTitle = "";
                if (transferSyntax != null)
                {
                    file.FileMetaInfo.AddOrUpdate(DicomTag.TransferSyntaxUID, transferSyntax);
                }
            }

            return file;
        }

        #region Reporting

        private string WrappingClass(AnonFunc f)
        {
            return f.Method.DeclaringType.FullName;
        }

        private string Name(AnonFunc f)
        {
            return f.Method.Name;
        }

        private string TagName(DicomTag t)
        {
            return t.DictionaryEntry.Name + " " + t.ToString().ToUpper();
        }

        private string Indent(int lvl)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < lvl; i++)
            {
                sb.Append("  ");
            }
            return sb.ToString();
        }

        private Dictionary<string, SortedDictionary<string, List<string>>> GroupTagsByTagFuncsAndTagHandler(IEnumerable<KeyValuePair<AnonFunc, string>> func_tag_stream)
        {
            // TagHandler => TagFunction => List<Tags>
            var grouped = new Dictionary<string, SortedDictionary<string, List<string>>>();

            foreach (var func_tag in func_tag_stream)
            {
                var func = func_tag.Key;
                var tag = func_tag.Value;

                // TagFunction => List<Tags>
                if (grouped.TryGetValue(WrappingClass(func), out SortedDictionary<string, List<string>> func_tags))
                {
                    if (!func_tags.ContainsKey(Name(func)))
                    {
                        func_tags[Name(func)] = new List<string> { tag };
                    }
                    else
                    {
                        func_tags[Name(func)].Add(tag);
                    }
                }
                else
                {
                    func_tags = new SortedDictionary<string, List<string>> { { Name(func), new List<string> { tag } } };
                    grouped.Add(WrappingClass(func), func_tags);
                }
            }

            return grouped;
        }

        // TODO: Return a well structured XML/JSON
        public List<string> ReportRegisteredHandlers()
        {
            var report = new List<string>();

            // By reverse iterating the registered TagHandlers, we can detect
            // configuration overwrites by checking if the same keys are
            // registered twice. We reverse iterating as the latest class
            // overwrites the previous ones.
            var seen = new HashSet<string>();
            var tmp_report = new List<List<string>>();
            foreach (var th in _tagHandlers.Reverse<ITagHandler>())
            {
                var conf = th.GetConfiguration();
                if (conf != null)
                {
                    var tmp_l = new List<string> { Indent(1) + "From: " + th.GetType().FullName };
                    foreach (var kv in conf)
                    {
                        if (!seen.Contains(kv.Key))
                        {
                            tmp_l.Add(Indent(2) + $"{kv.Key}: {kv.Value}");
                            seen.Add(kv.Key);
                        }
                    }
                    tmp_report.Add(tmp_l);
                }
            }
            // Reverse again, to report by registration order
            report.Add("Current Configuration:");
            tmp_report.Reverse<List<string>>().ToList().ForEach(l => report.AddRange(l));

            (new Dictionary<string, Dictionary<string, SortedDictionary<string, List<string>>>> {
                { "Regex", GroupTagsByTagFuncsAndTagHandler(_regexFuncs.Select(p => new KeyValuePair<AnonFunc, string>(p.Value, p.Key.ToString()))) },
                { "Tag", GroupTagsByTagFuncsAndTagHandler(_tagFuncs.Select(p => new KeyValuePair<AnonFunc, string>(p.Value, TagName(p.Key)))) }
            }).ToList()
            .ForEach(type_groups =>
            {
                var type = type_groups.Key;
                var groups = type_groups.Value;

                // 2 types: Regex and Tag Functions
                report.Add($"Current {type} Functions:");

                // We need to report TagHandler with the order they been registered
                _tagHandlers.ForEach(r =>
                {
                    var thName = r.GetType().FullName;

                    if (groups.ContainsKey(thName))
                    {
                        var func_tags = groups[thName];
                        report.Add("");
                        report.Add(Indent(1) + "From: " + thName);

                        func_tags.ToList().ForEach(kv2 =>
                        {
                            var tf = kv2.Key;

                            // Gets the description attribute of each AnonFunc
                            var method = r.GetType().GetMethod(tf);
                            var attr = (DescriptionAttribute)method.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
                            string desc = attr.Description;

                            // Get the related AnonFunc examples
                            var exampMethod = r.GetType().GetMethod(tf + "Examples");
                            var examples = exampMethod.Invoke(null, null) as List<AnonExample>;

                            var tags = kv2.Value;
                            tags.Sort();

                            report.Add("");
                            report.Add(Indent(2) + "Functionality: " + tf);
                            report.Add(Indent(2) + "Description:");
                            report.AddRange(Regex.Split(desc, "\r\n|\r|\n").Select(s => Indent(3) + s.Trim()));
                            report.Add(Indent(2) + "Examples:");
                            int num = 0;
                            examples.ForEach(ex =>
                            {
                                num++;
                                report.Add(Indent(3) + "Example " + num);
                                report.Add(Indent(4) + "Depending Input:");
                                report.Add(string.Join(Environment.NewLine, ex.DependingInput.Select(s => Indent(5) + s)));
                                report.Add(Indent(4) + "Input:");
                                report.Add(string.Join(Environment.NewLine, ex.Input.Select(s => Indent(5) + s)));
                                report.Add(Indent(4) + "Output:");
                                report.Add(string.Join(Environment.NewLine, ex.Output.Select(s => Indent(5) + s)));
                            });
                            report.Add(Indent(2) + "Tag List:");
                            report.AddRange(tags.Select(t => Indent(3) + t));
                        });
                    }
                });
                report.Add("");
            });

            return report;
        }

        public class AnonExample
        {
            public List<string> DependingInput { get; } = new List<string>();
            public List<string> Input { get; } = new List<string>();
            public List<string> Output { get; } = new List<string>();
            public List<string> StateBefore { get; } = new List<string>();
            public List<string> StateAfter { get; } = new List<string>();

            public static void InferOutput(DicomItem input, DicomItem output, AnonExample example)
            {
                if (output == null)
                {
                    example.Output.Add("<removed>");
                }
                else if (input == output)
                {
                    example.Output.Add("<retained>");
                }
                else if (output as DicomElement != null)
                {
                    if (output as DicomDate != null)
                    {
                        var v = (output as DicomDate).Get<DateTime>();
                        example.Output.Add(v.ToShortDateString());
                    }
                    else if (output as DicomTime != null)
                    {
                        var v = (output as DicomTime).Get<DateTime>();
                        example.Output.Add(v.TimeOfDay.ToString());
                    }
                    else
                    {
                        var v = (output as DicomElement).Get<string>();
                        example.Output.Add(v == "" ? "<empty>" : v);
                    }
                }
            }
        }

        #endregion

    }
}
