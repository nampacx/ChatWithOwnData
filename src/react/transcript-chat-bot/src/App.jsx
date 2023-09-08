import { useState, useEffect } from "react";
import lens from "./assets/lens.png";
import loadingGif from "./assets/loading.gif";
import "./App.css";

class Dialog {
  constructor(inputs, outputs) {
    this.inputs = inputs;
    this.outputs = outputs;
  }
}

function App() {
  const [prompt, updatePrompt] = useState(undefined);
  const [loading, setLoading] = useState(false);
  const [answer, setAnswer] = useState(undefined);
  const [chatHistory, setChatHistory] = useState([]);

  useEffect(() => {
    if (prompt != null && prompt.trim() === "") {
      setAnswer(undefined);
    }
  }, [prompt]);

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
        // "chat_history": chatHistory
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

      var dialog = {
        "inputs": {"question": prompt},
        "outputs": {"answer": answer}
      };

      setAnswer(answer);
      // setChatHistory([...chatHistory, dialog]);
      console.log(chatHistory);
    } catch (err) {
      console.error(err, "err");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app">
      <div className="app-container">
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
          <div className="spotlight__answer">{answer && <p>{answer}</p>}</div>
        </div>
      </div>
    </div>
  );
}

export default App;