import Chat from "./components/Chat";

function App() {
  // const apiUrl = "https://chat-ai-project.onrender.com"; 
  const apiUrl = "http://localhost:5135"; 
  return <Chat apiUrl={apiUrl} />;  
}

export default App;
