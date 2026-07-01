from pathlib import Path

from app.rag.document_loader import load_knowledge_documents


def test_load_knowledge_documents_reads_markdown_and_text_files(tmp_path: Path):
    knowledge_base = tmp_path / "knowledge-base"

    (knowledge_base / "npc").mkdir(parents=True)
    (knowledge_base / "lore").mkdir(parents=True)
    (knowledge_base / "npc" / "rock.md").write_text("# Rock\nSleepy guide.", encoding="utf-8")
    (knowledge_base / "lore" / "garden.txt").write_text("Outer garden.", encoding="utf-8")
    (knowledge_base / "lore" / ".gitkeep").write_text("", encoding="utf-8")

    documents = load_knowledge_documents(knowledge_base)

    assert [document.id for document in documents] == [
        "lore/garden.txt",
        "npc/rock.md",
    ]
    assert documents[0].category == "lore"
    assert documents[1].source == "rock.md"
