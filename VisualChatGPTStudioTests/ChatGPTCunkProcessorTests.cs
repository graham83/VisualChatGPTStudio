using JeffPires.VisualChatGPTStudio;
using JeffPires.VisualChatGPTStudio.Commands;


namespace VisualChatGPTStudioTests
{
    [TestFixture]
    public class ChatGPTChunkProcessorTests
    {
        [Test]
        public void TestBeforeCodeState()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = State.Summary;
            string chunk1 = "This is a test summary";
            string chunk2 = " for a method.";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);

            string expected = "/// This is a test summary for a method.\n";
            Assert.That(command.GetFormattedResponse(), Is.EqualTo(expected));
        }

        [Test]
        public void TestBeforeCodeWithNewLineState()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = State.Summary;
            string chunk1 = "This is a test summary\n";
            string chunk2 = " for a method.";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);

            string expected = "/// This is a test summary\n///  for a method.\n";
            var formattedResponse = command.GetFormattedResponse();
            Assert.That(formattedResponse, Is.EqualTo(expected));
        }

        [Test]
        public void TestCodeStartState()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = State.Summary;
            string chunk1 = "This is a test summary";
            string chunk2 = " for a method.";
            string chunk3 = "```";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);
            command.ProcessChunk(chunk3);

            string expected = "/// <summary>\n/// This is a test summary for a method.\n/// </summary>\n";
            Assert.That(command.GetFormattedResponse(), Is.EqualTo(expected));
        }
    }
}