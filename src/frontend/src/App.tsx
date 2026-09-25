import { useState } from "react";
import "./App.css";

function App() {
  const [message, setMessage] = useState("");
  const [response, setResponse] = useState("");
  const [loading, setLoading] = useState(false);

  const sendMessage = async () => {
    if (!message.trim()) {
      return;
    }

    setLoading(true);
    setResponse("");

    try {
      const result = await fetch("http://localhost:5056/api/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          message: message,
        }),
      });

      if (!result.ok) {
        throw new Error("Failed to call Copilot API");
      }

      const data = await result.json();

      setResponse(data.message);
    } catch (error) {
      console.error(error);
      setResponse("Unable to connect to the Copilot API.");
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
      </header>

      <main className="chat-container">
        <div className="messages">
          <div className="message assistant">
            <div className="message-role">Copilot</div>

            <div className="message-content">
              Hello! I'm your AI Software Engineer Copilot.
              <br />
              Ask me anything about software engineering.
            </div>
          </div>

          {response && (
            <div className="message assistant">
              <div className="message-role">Copilot</div>

              <div className="message-content">
                {response}
              </div>
            </div>
          )}
        </div>

        <div className="input-container">
          <textarea
            value={message}
            onChange={(event) => setMessage(event.target.value)}
            placeholder="Ask the Copilot something..."
            rows={3}
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
