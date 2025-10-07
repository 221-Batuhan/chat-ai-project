import Chat from "./components/Chat";

function App() {
  //const apiUrl = "chat-ai-project-production.up.railway.app";
  const apiUrl = "http://localhost:5135";
  return <Chat apiUrl={apiUrl} />;  
}

export default App;
