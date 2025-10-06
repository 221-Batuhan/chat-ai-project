import { useState, useEffect } from "react";

export default function Chat({ apiUrl }) {
  const [messages, setMessages] = useState([]);
  const [text, setText] = useState("");

  useEffect(() => {
    fetchMessages();
  }, []);

  async function fetchMessages() {
    try {
      const res = await fetch(`${apiUrl}/api/messages`);
      const data = await res.json();
      setMessages(data.reverse());
    } catch (err) {
      console.error("Fetch error:", err);
    }
  }

  async function sendMessage() {
    if (!text) return;
    try {
      await fetch(`${apiUrl}/api/messages`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nickname: "anon", text }),
      });
      setText("");
      fetchMessages();
    } catch (err) {
      console.error("Send error:", err);
    }
  }

  return (
    <div style={{ maxWidth: 500, margin: "auto", fontFamily: "Arial" }}>
      <h1>Chat App</h1>
      <div
        style={{
          border: "1px solid gray",
          padding: 10,
          minHeight: 300,
          overflowY: "auto",
          marginBottom: 10,
        }}
      >
        {messages.map((m) => (
          <div key={m.id} style={{ marginBottom: 5 }}>
            <b>{m.nickname}:</b> {m.text} <i>({m.sentiment})</i>
          </div>
        ))}
      </div>
      <input
        type="text"
        value={text}
        onChange={(e) => setText(e.target.value)}
        onKeyDown={(e) => e.key === "Enter" && sendMessage()}
        style={{ width: "80%", padding: 5 }}
      />
      <button onClick={sendMessage} style={{ padding: 5, marginLeft: 5 }}>
        Send
      </button>
    </div>
  );
}
