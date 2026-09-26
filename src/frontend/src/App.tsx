import { useEffect, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter";
import { vscDarkPlus } from "react-syntax-highlighter/dist/esm/styles/prism";
import "./App.css";

type Message = {
  role: "user" | "assistant";
  content: string;
};

const initialMessage: Message = {
  role: "assistant",
  content:
    "Hello! I'm your AI Software Engineer Copilot. Ask me anything about software engineering.",
};

function App() {
  const messagesContainerRef = useRef<HTMLDivElement>(null);

  const [message, setMessage] = useState("");

  const [messages, setMessages] = useState<Message[]>([
    initialMessage,
  ]);

  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const container = messagesContainerRef.current;

    if (!container) {
      return;
    }

    container.scrollTop = container.scrollHeight;
  }, [messages]);

  const updateAssistantMessage = (content: string) => {
    setMessages((currentMessages) => {
      const updated = [...currentMessages];

      const lastMessageIndex = updated.length - 1;

      if (
        lastMessageIndex >= 0 &&
        updated[lastMessageIndex].role === "assistant"
      ) {
        updated[lastMessageIndex] = {
          role: "assistant",
          content,
        };
      }

      return updated;
    });
  };

  const decodeBase64 = (encodedData: string): string => {
    const binaryString = atob(encodedData);

    const bytes = Uint8Array.from(
      binaryString,
      (character) => character.charCodeAt(0)
    );

    return new TextDecoder().decode(bytes);
  };

  const clearConversation = () => {
    if (loading) {
      return;
    }

    setMessages([initialMessage]);
    setMessage("");
  };

  const sendMessage = async () => {
    if (!message.trim() || loading) {
      return;
    }

    const userMessage: Message = {
      role: "user",
      content: message.trim(),
    };

    const updatedMessages = [...messages, userMessage];

    setMessages(updatedMessages);
    setMessage("");
    setLoading(true);

    try {
      const response = await fetch(
        "http://localhost:5056/api/chat/stream",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            messages: updatedMessages,
          }),
        }
      );

      if (!response.ok) {
        let errorMessage =
          "The Copilot was unable to process your request.";

        try {
          const errorBody = await response.json();

          if (errorBody?.detail) {
            errorMessage = errorBody.detail;
          } else if (errorBody?.error) {
            errorMessage = errorBody.error;
          }
        } catch {
          // Ignore JSON parsing errors and use the default message.
        }

        throw new Error(errorMessage);
      }

      if (!response.body) {
        throw new Error(
          "The Copilot returned an empty response."
        );
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();

      let assistantContent = "";
      let buffer = "";

      setMessages((currentMessages) => [
        ...currentMessages,
        {
          role: "assistant",
          content: "",
        },
      ]);

      while (true) {
        const { value, done } = await reader.read();

        if (done) {
          break;
        }

        buffer += decoder.decode(value, {
          stream: true,
        });

        const events = buffer.split("\n\n");

        buffer = events.pop() ?? "";

        for (const event of events) {
          const lines = event.split("\n");

          for (const line of lines) {
            if (!line.startsWith("data:")) {
              continue;
            }

            const encodedData = line
              .substring(5)
              .replace(/^ /, "");

            if (encodedData === "[DONE]") {
              continue;
            }

            const data = decodeBase64(encodedData);

            assistantContent += data;

            updateAssistantMessage(assistantContent);
          }
        }
      }

      if (buffer.trim()) {
        const lines = buffer.split("\n");

        for (const line of lines) {
          if (!line.startsWith("data:")) {
            continue;
          }

          const encodedData = line
            .substring(5)
            .replace(/^ /, "");

          if (encodedData === "[DONE]") {
            continue;
          }

          const data = decodeBase64(encodedData);

          assistantContent += data;

          updateAssistantMessage(assistantContent);
        }
      }
    } catch (error) {
      console.error("Copilot request failed:", error);

      const errorMessage =
        error instanceof Error
          ? error.message
          : "The Copilot was unable to process your request.";

      setMessages((currentMessages) => [
        ...currentMessages,
        {
          role: "assistant",
          content: `⚠️ **Error:** ${errorMessage}`,
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app">
      <header className="header">
        <div>
          <h1>AI Software Engineer Copilot</h1>
          <p>Your AI assistant for software engineering</p>
        </div>

        <button
          onClick={clearConversation}
          disabled={loading}
        >
          Clear Chat
        </button>
      </header>

      <main className="chat-container">
        <div
          className="messages"
          ref={messagesContainerRef}
        >
          {messages.map((item, index) => (
            <div
              key={index}
              className={`message ${item.role}`}
            >
              <div className="message-role">
                {item.role === "user"
                  ? "You"
                  : "Copilot"}
              </div>

              <div className="message-content">
                <ReactMarkdown
                  components={{
                    code({
                      className,
                      children,
                      ...props
                    }) {
                      const match = /language-(\w+)/.exec(
                        className || ""
                      );

                      const isInline = !match;

                      return !isInline ? (
                        <SyntaxHighlighter
                          style={vscDarkPlus}
                          language={match[1]}
                          PreTag="div"
                        >
                          {String(children).replace(/\n$/, "")}
                        </SyntaxHighlighter>
                      ) : (
                        <code
                          className={className}
                          {...props}
                        >
                          {children}
                        </code>
                      );
                    },
                  }}
                >
                  {item.content}
                </ReactMarkdown>
              </div>
            </div>
          ))}
        </div>

        <div className="input-container">
          <textarea
            value={message}
            onChange={(event) =>
              setMessage(event.target.value)
            }
            onKeyDown={(event) => {
              if (
                event.key === "Enter" &&
                !event.shiftKey
              ) {
                event.preventDefault();
                sendMessage();
              }
            }}
            placeholder="Ask the Copilot something..."
            rows={3}
            disabled={loading}
          />

          <button
            onClick={sendMessage}
            disabled={!message.trim() || loading}
          >
            {loading ? "Sending..." : "Send"}
          </button>
        </div>
      </main>
    </div>
  );
}

export default App;