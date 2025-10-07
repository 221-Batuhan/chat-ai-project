import React, { useState, useEffect, useRef } from "react";
import "./Chat.css";

export default function Chat({ apiUrl }) {
  const [messages, setMessages] = useState([]);
  const [text, setText] = useState("");
  const [sending, setSending] = useState(false);
  const [username, setUsername] = useState(() =>
    localStorage.getItem("chat_username") || ""
  );
  const [editingName, setEditingName] = useState(!localStorage.getItem("chat_username"));
  const messagesEndRef = useRef(null);

  const fetchMessages = async () => {
    const res = await fetch(`${apiUrl}/api/messages?t=${Date.now()}`, {
      cache: "no-store",
    });
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
    if (!username.trim()) {
      setEditingName(true);
      return;
    }
    setSending(true);
    try {
      const res = await fetch(`${apiUrl}/api/messages`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        nickname: username.trim(),
        text,
      }),
      });
      if (!res.ok) throw new Error(`POST failed: ${res.status}`);
      const created = await res.json();
      // optimistic append so message doesn't flicker/disappear
      setMessages((prev) => [created, ...prev]);
      setText("");
      // kick a refresh to get updated sentiment shortly after
      setTimeout(fetchMessages, 1200);
    } catch (e) {
      console.error(e);
    } finally {
      setSending(false);
    }
  };

  return (
    <div className="chat-container">
      <h2 className="chat-title">Chat AI</h2>
      <div className="chat-username-bar">
        {editingName ? (
          <div className="username-editor">
            <input
              className="username-input"
              value={username}
              placeholder="Kullanıcı adınız"
              onChange={(e) => setUsername(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  const v = username.trim();
                  if (v) {
                    (async () => {
                      try {
                        await fetch(`${apiUrl}/api/users/register`, {
                          method: "POST",
                          headers: { "Content-Type": "application/json" },
                          body: JSON.stringify({ username: v }),
                        });
                      } catch {}
                      localStorage.setItem("chat_username", v);
                      setEditingName(false);
                    })();
                  }
                }
              }}
            />
            <button
              className="username-save"
              onClick={async () => {
                const v = username.trim();
                if (v) {
                  try {
                    await fetch(`${apiUrl}/api/users/register`, {
                      method: "POST",
                      headers: { "Content-Type": "application/json" },
                      body: JSON.stringify({ username: v }),
                    });
                  } catch {}
                  localStorage.setItem("chat_username", v);
                  setEditingName(false);
                }
              }}
            >
              Kaydet
            </button>
          </div>
        ) : (
          <div className="username-display">
            <span className="username-pill">{username}</span>
            <button className="username-edit" onClick={() => setEditingName(true)}>
              Değiştir
            </button>
          </div>
        )}
      </div>
      <div className="chat-messages">
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`chat-message ${
              username && msg.nickname === username ? "own-message" : ""
            }`}
          >
            <div className="chat-avatar">
              {(msg.nickname || "?")[0].toUpperCase()}
            </div>
            <div className="chat-content">
              <div className="chat-header">
                <span className="chat-nickname">{msg.nickname}</span>
                <span className={`chat-sentiment ${msg.sentiment || "pending"}`} title="Duygu Analizi">
                  {msg.sentiment || "analysing"}
                </span>
                <span className="chat-time" title={msg.createdAt}>
                  {new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
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
        <button className="chat-send-btn" onClick={sendMessage} disabled={sending}>
          Gönder
        </button>
      </div>
    </div>
  );
}