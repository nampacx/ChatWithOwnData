import { useState, useEffect } from "react";
import lens from "./assets/lens.png";
import loadingGif from "./assets/loading.gif";
import "./App.css";

function App() {
  const [prompt, updatePrompt] = useState(undefined);
  const [loading, setLoading] = useState(false);
  const [chatHistory, setChatHistory] = useState([]);


  useEffect(() => {
    if(chatHistory.length > 0) {
    var chatElement = document.getElementById("chat");
    chatElement.scrollTop = chatElement.scrollHeight;
  }}, [chatHistory]);

  const sendPrompt = async (event) => {
    if (event.key !== "Enter") {
      return;
    }

    try {
      setLoading(true);
      var myHeaders = new Headers();
      myHeaders.append("Content-Type", "application/json");

      var raw = JSON.stringify({
        "question": prompt,
        "index_name": "architectcommunity",
        "chat_history": chatHistory
      });

      var requestOptions = {
        method: 'POST',
        headers: myHeaders,
        body: raw,
        redirect: 'follow'
      };

      const res = await fetch("http://localhost:7071/api/ask", requestOptions);
      console.log(res, "res");
      if (!res.ok) {
        throw new Error("Something went wrong");
      }

      const { answer } = await res.json();
      console.log(answer);

      var user = { role: "user", content: prompt };
      var system = { role: "assistant", content: answer };

      setChatHistory([...chatHistory, user, system]);
    } catch (err) {
      console.error(err, "err");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app">
      <div className="app-container">

            <div className="chat" id="chat" >
              <section>
                {chatHistory && chatHistory.length
                  ? chatHistory.map((chat, index) => (
                    <div className={chat.role === "user" ? "user_msg" : "sys_msg"}>
                      <p key={index} className={chat.role === "user" ? "user_msg" : ""}>
                        {chat.content}
                      </p>
                    </div>
                  ))
                  : ""}
              </section>
            </div>
            <div className="spotlight__wrapper">
              <input
                type="text"
                className="spotlight__input"
                placeholder="Ask me anything..."
                disabled={loading}
                style={{
                  backgroundImage: loading ? `url(${loadingGif})` : `url(${lens})`,
                }}
                onChange={(e) => updatePrompt(e.target.value)}
                onKeyDown={(e) => sendPrompt(e)}
              />
            </div>
              </div>
    </div>
  );
}

export default App;