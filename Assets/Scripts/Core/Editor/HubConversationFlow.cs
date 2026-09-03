using System.Collections.Generic;
using System.Linq;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Editor
{
    // A conversation read as the shape it actually has: runs of lines, and where each run goes.
    //
    // Branching is already stored as labels and target labels, so this is a view of the data rather
    // than anything new — nothing has to be migrated for a script to be drawn.
    public static class HubConversationFlow
    {
        // Where a run of lines goes when it ends.
        public readonly struct Exit
        {
            public readonly string Reads;
            public readonly string TargetLabel;
            public readonly bool Dangling;

            public Exit(string reads, string targetLabel, bool dangling)
            {
                Reads = reads;
                TargetLabel = targetLabel;
                Dangling = dangling;
            }

            // An exit with no target is the end of the conversation.
            public bool Ends => string.IsNullOrEmpty(TargetLabel);
        }

        // One run of lines, from a label to just before the next one.
        public sealed class Block
        {
            public string Label;
            public int First;
            public int Count;
            public readonly List<Exit> Exits = new();

            // Lines after a jump or a choice in the same run, which nothing can ever reach.
            public int Unreachable;

            public string Name => string.IsNullOrEmpty(Label) ? "the opening" : Label;
        }

        public static IReadOnlyList<Block> Read(DialogueScript script)
        {
            if (script == null) return System.Array.Empty<Block>();

            List<Block> blocks = Split(script);
            foreach (Block block in blocks) Trace(script, blocks, block);
            return blocks;
        }

        // Everything wrong with the way it branches, in the words a writer would use.
        public static IReadOnlyList<string> Problems(DialogueScript script)
        {
            var wrong = new List<string>();

            foreach (Block block in Read(script))
            {
                foreach (Exit exit in block.Exits.Where(exit => exit.Dangling))
                    wrong.Add($"{block.Name}: nothing is labelled '{exit.TargetLabel}'");

                if (block.Unreachable > 0)
                    wrong.Add($"{block.Name}: {block.Unreachable} " +
                              $"{(block.Unreachable == 1 ? "line comes" : "lines come")} after it has already " +
                              "branched away, so nothing reaches them");
            }

            foreach (string repeated in RepeatedLabels(script))
                wrong.Add($"two entries are both labelled '{repeated}', so only the first can be reached");

            return wrong;
        }

        private static List<Block> Split(DialogueScript script)
        {
            var blocks = new List<Block>();
            IReadOnlyList<DialogueNode> nodes = script.Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                bool starts = i == 0 || !string.IsNullOrEmpty(nodes[i].Label);
                if (starts) blocks.Add(new Block { Label = nodes[i].Label, First = i });

                blocks[^1].Count++;
            }
            return blocks;
        }

        // Where this run goes is decided by the first thing in it that branches, or by simply
        // running on into the next run.
        private static void Trace(DialogueScript script, List<Block> blocks, Block block)
        {
            IReadOnlyList<DialogueNode> nodes = script.Nodes;

            for (int i = block.First; i < block.First + block.Count; i++)
            {
                DialogueNode node = nodes[i];
                if (node.Kind != DialogueNodeKind.Jump && node.Kind != DialogueNodeKind.Choice) continue;

                AddBranches(script, block, node);
                block.Unreachable = block.First + block.Count - i - 1;
                return;
            }

            RunOn(blocks, block);
        }

        private static void AddBranches(DialogueScript script, Block block, DialogueNode node)
        {
            if (node.Kind == DialogueNodeKind.Jump)
            {
                block.Exits.Add(Leading(script, "goes to", node.TargetLabel));
                return;
            }

            foreach (DialogueOption option in node.Options)
                block.Exits.Add(Leading(script, $"“{option.Label}”", option.TargetLabel));

            if (node.AllowCancel) block.Exits.Add(Leading(script, "backs out", node.CancelTargetLabel));
        }

        private static void RunOn(List<Block> blocks, Block block)
        {
            int next = blocks.IndexOf(block) + 1;
            block.Exits.Add(next < blocks.Count
                ? new Exit("runs on into", blocks[next].Label, dangling: false)
                : new Exit("nothing follows", null, dangling: false));
        }

        // A target nothing carries is a branch that ends the conversation instead of going where
        // it says, so it is marked rather than drawn like any other.
        private static Exit Leading(DialogueScript script, string reads, string target) =>
            new(reads, target, !string.IsNullOrEmpty(target) && script.IndexOfLabel(target) < 0);

        private static IEnumerable<string> RepeatedLabels(DialogueScript script)
        {
            var seen = new HashSet<string>();
            var twice = new HashSet<string>();

            foreach (DialogueNode node in script.Nodes)
                if (!string.IsNullOrEmpty(node.Label) && !seen.Add(node.Label)) twice.Add(node.Label);

            return twice;
        }
    }
}
