import grpc from '@grpc/grpc-js';
import protoLoader from '@grpc/proto-loader';
import puppeteer from 'puppeteer';

// Initialize Puppeteer browser (reuse across requests)
let browser;
(async () => {
    browser = await puppeteer.launch();
})();

async function validateSyntax(call, callback) {
    const syntax = call.request.syntax;

    try {
        const page = await browser.newPage();

        // Inject the Mermaid.js script from the CDN
        await page.addScriptTag({
            url: 'https://cdnjs.cloudflare.com/ajax/libs/mermaid/11.4.0/mermaid.min.js'
        });

        // Evaluate the syntax in the browser context
        const result = await page.evaluate(async (syntax) => {
            try {
                // Initialize Mermaid
                mermaid.initialize({ startOnLoad: false });
                await mermaid.parse(syntax, { suppressErrors: false });

                return { valid: true, message: 'The Mermaid syntax is valid.' };
            } catch (error) {
                return { valid: false, message: error.message || 'Invalid Mermaid syntax.' };
            }
        }, syntax);

        // Close the page
        await page.close();

        // Log the response for debugging
        console.log('Response:', result);
        callback(null, result);
    } catch (error) {
        console.error('Server error:', error.message);
        callback(null, { valid: false, message: error.message || 'An unexpected error occurred.' });
    }
}

// Load the .proto file
const packageDefinition = protoLoader.loadSync('Protos/mermaid.proto', {
    keepCase: true,
    longs: String,
    enums: String,
    defaults: true,
    oneofs: true
});
const mermaidProto = grpc.loadPackageDefinition(packageDefinition).mermaid;

function main() {
    const server = new grpc.Server();
    server.addService(mermaidProto.MermaidService.service, { ValidateSyntax: validateSyntax });
    server.bindAsync('0.0.0.0:50051', grpc.ServerCredentials.createInsecure(), () => {
        console.log('Node.js gRPC server running on port 50051');
    });
}

main();