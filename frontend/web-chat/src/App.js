import Chat from "./components/Chat";

function App() {
  const apiUrl = "https://chat-ai-project.onrender.com"; 
  return <Chat apiUrl={apiUrl} />;  
}

export default App;
