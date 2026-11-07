import path from "node:path";
import js from "@eslint/js";
import reactHooks from "eslint-plugin-react-hooks";
import tseslint from "typescript-eslint";

const featuresDirectory = path.resolve(import.meta.dirname, "src", "features");

function resolveImport(source, filename) {
  if (source.startsWith("@/"))
    return path.resolve(import.meta.dirname, "src", source.slice(2));
  if (source.startsWith("."))
    return path.resolve(path.dirname(filename), source);
  return null;
}

function featurePath(absolutePath) {
  const relative = path.relative(featuresDirectory, absolutePath);
  if (relative === "" || relative.startsWith("..") || path.isAbsolute(relative))
    return null;
  const [feature, ...rest] = relative.split(path.sep);
  return {
    feature,
    isPublicEntry:
      rest.length === 0 ||
      rest.join("/") === "index" ||
      rest.join("/") === "index.ts",
  };
}

// Uma feature só pode importar outra pela interface pública (index.ts).
const featureBoundaries = {
  meta: {
    type: "problem",
    messages: {
      internal:
        "A feature '{{from}}' só pode importar '{{to}}' por '@/features/{{to}}'.",
    },
  },
  create(context) {
    const from = featurePath(context.filename);
    if (!from) return {};

    const check = (node) => {
      const source = node.source;
      if (source?.type !== "Literal" || typeof source.value !== "string")
        return;
      const resolved = resolveImport(source.value, context.filename);
      const to = resolved && featurePath(resolved);
      if (to && to.feature !== from.feature && !to.isPublicEntry) {
        context.report({
          node: source,
          messageId: "internal",
          data: { from: from.feature, to: to.feature },
        });
      }
    };

    return {
      ImportDeclaration: check,
      ImportExpression: check,
      ExportNamedDeclaration: check,
      ExportAllDeclaration: check,
    };
  },
};

export default tseslint.config(
  { ignores: ["node_modules"] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    // Roda no contexto do Service Worker, não do DOM.
    files: ["sw.js"],
    languageOptions: {
      globals: {
        self: "readonly",
        caches: "readonly",
        fetch: "readonly",
        Request: "readonly",
        URL: "readonly",
      },
    },
  },
  {
    files: ["src/**/*.{ts,tsx}"],
    plugins: {
      "react-hooks": reactHooks,
      wrexercise: { rules: { "feature-boundaries": featureBoundaries } },
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "wrexercise/feature-boundaries": "error",
      "no-restricted-globals": [
        "error",
        {
          name: "localStorage",
          message:
            "Credenciais e tokens nunca ficam em localStorage. Ver Wrexercise/webapp/AGENTS.md.",
        },
      ],
    },
  },
);
