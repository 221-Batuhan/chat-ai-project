import React, { useState, useEffect, useRef } from "react";
import "./Chat.css";

export default function Chat() {
  const [messages, setMessages] = useState([]);
  const [text, setText] = useState("");
  const messagesEndRef = useRef(null);

  const fetchMessages = async () => {
    const res = await fetch("https://chat-ai-project.onrender.com/api/messages");
    const data = await res.json();
    setMessages(data);
  };

  useEffect(() => {
    fetchMessages();
    const interval = setInterval(fetchMessages, 2000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const sendMessage = async () => {
    if (!text.trim()) return;
    await fetch("https://chat-ai-project.onrender.com/api/messages", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        nickname: "batuhan",
        text,
        sentiment: "neutral",
      }),
    });
    setText("");
    fetchMessages();
  };

  return (
    <div className="chat-container">
      <h2 className="chat-title">Chat AI</h2>
      <div className="chat-messages">
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`chat-message ${
              msg.nickname === "batuhan" ? "own-message" : ""
            }`}
          >
            <div className="chat-avatar">
              {msg.nickname[0].toUpperCase()}
            </div>
            <div className="chat-content">
              <div className="chat-header">
                <span className="chat-nickname">{msg.nickname}</span>
                <span
                  className={`chat-sentiment ${msg.sentiment}`}
                  title="Duygu Analizi"
                >
                  {msg.sentiment}
                </span>
              </div>
              <div className="chat-text">{msg.text}</div>
            </div>
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>
      <div className="chat-input-area">
        <input
          className="chat-input"
          type="text"
          placeholder="Mesajınızı yazın..."
          value={text}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && sendMessage()}
        />
        <button className="chat-send-btn" onClick={sendMessage}>
          Gönder
        </button>
      </div>
    </div>
  );
}