import { useState } from 'react';
import './FormComponent.css';

function FormComponent() {
    const [messages, setMessages] = useState([]);
    const [query, setQuery] = useState('');

    const handleSubmit = async (event) => {
        event.preventDefault();

        setMessages((prevMessages) => [...prevMessages, { text: query, from: 'user' }]);
        setQuery('');

        try {
            const apiUrl = 'https://localhost:55008/Gemini/';
            
            const response = await fetch(apiUrl, {
                method: 'POST',
                body: JSON.stringify({
                    contents: [
                        {
                            role: 'user',
                            parts: [{ text: query }]
                        }
                    ]
                }),
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            if (!response.ok) {
                throw new Error(`API Error: ${response.status}`);
            }

            const responseData = await response.json();

            setMessages((prevMessages) => [...prevMessages, { text: responseData.text, from: 'gemini' }]);
        } catch (error) {
            console.error('Error fetching data:', error);
            setMessages((prevMessages) => [...prevMessages, { text: 'Error: Unable to reach API. Check your network and API key.', from: 'gemini' }]);
        }
    };

    return (
        <div>
            <div className="background-gradient"></div>
            <div>
                <h1 className="title">Welcome to my portfolio</h1>
                <p className="subheader">Ask any questions about me and my experience</p>
            </div>
            <div className="chat-container">
                <div className="chat-window">
                    {messages.map((message, index) => (
                        <div key={index} className={`message ${message.from}`}>
                            {message.text}
                        </div>
                    ))}
                </div>
                <form onSubmit={handleSubmit} className="input-area">
                    <input
                        type="text"
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        placeholder="Ask your question..."
                    />
                    <button type="submit">Send</button>
                </form>
            </div>
        </div>
    );
}

export default FormComponent;
