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
            command.CurrentState = ChatResponseState.Summary;
            string chunk1 = "This is a test summary";
            string chunk2 = " for a method.";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);

            string expected = "/// <summary>\n/// This is a test summary for a method.\n/// </summary>\n";
            var formattedResponse = command.GetFormattedResponse();
            Assert.That(formattedResponse, Is.EqualTo(expected));
        }

        [Test]
        public void TestBeforeCodeWithNewLineState()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = ChatResponseState.Summary;
            string chunk1 = "This is a test summary\n";
            string chunk2 = " for a method.";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);

            string expected = "/// <summary>\n/// This is a test summary\n///  for a method.\n/// </summary>\n";
            var formattedResponse = command.GetFormattedResponse();
            Assert.That(formattedResponse, Is.EqualTo(expected));
        }

        [Test]
        public void TestCodeStartState()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = ChatResponseState.Summary;
            string chunk1 = "This is a test summary";
            string chunk2 = " for a method.";
            string chunk3 = "```";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);
            command.ProcessChunk(chunk3);

            string expected = "/// <summary>\n/// This is a test summary for a method.\n/// </summary>\n";
            var formattedResponse = command.GetFormattedResponse();
            Assert.That(formattedResponse, Is.EqualTo(expected));
        }

        [Test]
        public void TestCodeStartState2()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = ChatResponseState.Summary;
            string chunk1 = "This is a test summary";
            string chunk2 = " for a method.";
            string chunk3 = " Now here is the code`";
            string chunk4 = "``";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);
            command.ProcessChunk(chunk3);
            command.ProcessChunk(chunk4);

            string expected = "/// <summary>\n/// This is a test summary for a method. Now here is the code\n/// </summary>\n";
            var formattedResponse = command.GetFormattedResponse();
            Assert.That(formattedResponse, Is.EqualTo(expected));
        }


        [Test]
        public void TestCodeStartState3()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = ChatResponseState.Summary;
            string chunk1 = "This is a test summary";
            string chunk2 = " for a method.";
            string chunk3 = " Now here is the code```";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);
            command.ProcessChunk(chunk3);


            string expected = "/// <summary>\n/// This is a test summary for a method. Now here is the code\n/// </summary>\n";
            var formattedResponse = command.GetFormattedResponse();
            Assert.That(formattedResponse, Is.EqualTo(expected));
        }

        [Test]
        public void TestCurrentlyCoding()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = ChatResponseState.Summary;
            string chunk1 = "This is a test summary```";
            string chunk2 = "public FooBar()\n";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);

            string expected = "/// <summary>\n/// This is a test summary\n/// </summary>\npublic FooBar()\n";
            var formattedResponse = command.GetFormattedResponse();
            Assert.That(formattedResponse, Is.EqualTo(expected));
        }

        [Test]
        public void TestCodingComplete()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = ChatResponseState.Summary;
            string chunk1 = "This is a test summary```";
            string chunk2 = "public FooBar()\n";
            string chunk3 = "{}\n```";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);
            command.ProcessChunk(chunk3);

            string expected = "/// <summary>\n/// This is a test summary\n/// </summary>\npublic FooBar()\n{}\n/// <remarks>\n/// \n/// </remarks>\n";
            var formattedResponse = command.GetFormattedResponse();
            Assert.That(formattedResponse, Is.EqualTo(expected));
        }

        [Test]
        public void TestCodingRemarkse()
        {
            var command = new ChatGPTChunkProcessor();
            command.CurrentState = ChatResponseState.Summary;
            string chunk1 = "This is a test summary```";
            string chunk2 = "public FooBar()\n{}\n```";
            string chunk3 = "And thats how you code it.";

            command.ProcessChunk(chunk1);
            command.ProcessChunk(chunk2);
            command.ProcessChunk(chunk3);

            string expected = "/// <summary>\n/// This is a test summary\n/// </summary>\npublic FooBar()\n{}\n/// <remarks>\n/// And thats how you code it.\n/// </remarks>\n";
            var formattedResponse = command.GetFormattedResponse();
            Assert.That(formattedResponse, Is.EqualTo(expected));
        }
    }
}