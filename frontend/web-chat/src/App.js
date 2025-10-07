import Chat from "./components/Chat";

function App() {
  // In development, default to local backend; otherwise use env or Render URL
  const apiUrl =
    (process.env.NODE_ENV === "development"
      ? (process.env.REACT_APP_API_URL || "http://localhost:5135")
      : (process.env.REACT_APP_API_URL || "https://chat-ai-project.onrender.com"));
  return <Chat apiUrl={apiUrl} />;
}

export default App;
