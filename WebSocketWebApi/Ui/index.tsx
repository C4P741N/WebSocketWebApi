import React, { useState, useEffect, useRef } from "react";

const WebSocketComponent: React.FC = () => {
  const [messages, setMessages] = useState<string[]>([]);
  const wsRef = useRef<WebSocket | null>(null);

  useEffect(() => {
    // Establish WebSocket connection to the server
    wsRef.current = new WebSocket("ws://localhost:5000/ws");

    wsRef.current.onopen = () => {
      console.log("WebSocket connected");
    };

    wsRef.current.onmessage = (event: MessageEvent) => {
      // Handle incoming messages from the server
      setMessages((prevMessages) => [...prevMessages, event.data]);
    };

    wsRef.current.onclose = () => {
      console.log("WebSocket disconnected");
    };

    return () => {
      // Clean up WebSocket connection when the component is unmounted
      if (wsRef.current) {
        wsRef.current.close();
      }
    };
  }, []);

  return (
    <div>
      <h1>Server Messages</h1>
      <div>
        <h3>Messages from Server:</h3>
        <ul>
          {messages.map((msg, index) => (
            <li key={index}>{msg}</li>
          ))}
        </ul>
      </div>
    </div>
  );
};

export default WebSocketComponent;
