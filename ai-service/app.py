import gradio as gr
from transformers import pipeline

model = pipeline("sentiment-analysis", model="savasy/bert-base-turkish-sentiment-cased")

def analyze(text):
    if not text:
        return {"label": "neutral", "score": 0.5}
    result = model(text[:512])[0]
    label = result["label"].lower()
    score = result["score"]
    if "pos" in label:
        label = "positive"
    elif "neg" in label:
        label = "negative"
    else:
        label = "neutral"
    return {"label": label, "score": score}

iface = gr.Interface(fn=analyze, inputs=gr.Textbox(lines=2, placeholder="Type message..."), outputs="json")
if __name__ == "__main__":
    iface.launch()
