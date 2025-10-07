import Chat from "./components/Chat";

function App() {
  const apiUrl = "chat-ai-project-production.up.railway.app:8080";
  return <Chat apiUrl={apiUrl} />;  
}

export default App;
