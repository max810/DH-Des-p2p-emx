import json
import logging

import threading
from websocket_server import WebsocketServer
from logic import *
from time import sleep
# from flask_cors import CORS
# from flask_socketio import SocketIO, emit
# from flask_sockets import Sockets
import base64

usernames = dict()
server_private_keys = dict()
server_new_private_keys = dict()
client_public_keys = dict()


def encode_public_keynumbers(pbkn):
    e = base64.b64encode(int_to_bytes(pbkn.e)).decode('ascii')
    n = base64.b64encode(int_to_bytes(pbkn.n)).decode('ascii')
    return e, n


def decode_public_keynumbers(eb64, nb64):
    eb = base64.b64decode(eb64)
    nb = base64.b64decode(nb64)
    e = int_from_bytes(eb)
    n = int_from_bytes(nb)
    return e, n


def int_to_bytes(x):
    return x.to_bytes((x.bit_length() + 7) // 8, 'big')


def int_from_bytes(xbytes):
    return int.from_bytes(xbytes, 'big')


def format_message(event, data, message_blocks=[]):
    message = {
        "event_type": event,
        "message_blocks": message_blocks,
        "data": data
    }
    return json.dumps(message)


# app = Flask(__name__)
# app.debug = True
# sockets = Sockets(app)
def on_new_client(client, server):
    print(client['id'], client['address'], sep='|')


def on_message(client, server, message):
    msg = json.loads(message)
    event = msg["event_type"]
    handler = handlers[event]
    handler(client, msg)


def on_register(client, msg):
    data = msg['data']
    client_id = client['id']
    if client_id in usernames:
        server.send_message(client, format_message('registration_fail', {}))
        return
    username = data['username']
    eb64, nb64 = data['e'], data['n']

    usernames[client_id] = username

    e, n = decode_public_keynumbers(eb64, nb64)
    userPbk = create_public_key(e, n)
    client_public_keys[client_id] = userPbk

    server_prk = generate_new_private_key()
    server_private_keys[client_id] = server_prk
    e_serverb64, n_serverb64 = encode_public_keynumbers(
        server_prk.public_key().public_numbers()
    )

    server.send_message(
        client,
        format_message(
            'registration_success',
            {
                'e': e_serverb64,
                'n': n_serverb64
            }
        )
    )

    server.send_message_to_all(
        format_message(
            'client_joined_chat',
            {
                'username': username
            }
        )
    )

    lock = threading.Lock()
    lock.acquire()
    server_new_private_keys[client_id] = server_prk
    lock.release()


def on_check_username(client, msg):
    data = msg['data']
    username = data['username']
    if username in usernames.values():
        server.send_message(client, format_message('username_taken', {}))
    else:
        server.send_message(client, format_message('username_available', {}))


def on_chat_message(client, msg):
    data = msg['data']
    client_id = client['id']
    username = usernames[client_id]

    eb64, nb64 = data['e'], data['n']
    e, n = decode_public_keynumbers(eb64, nb64)
    client_public_keys[client_id] = create_public_key(e, n)
    private_key = server_private_keys[client_id]

    message_blocks = msg['message_blocks']
    message_bytes = []
    for block in message_blocks:
        block_decoded = base64.b64decode(block)
        block_decrypted = decrypt(block_decoded, private_key)
        message_bytes += block_decrypted

    for curr_client in server.clients:
        curr_client_id = curr_client['id']
        if curr_client_id not in usernames:
            continue
        curr_client_pbk = client_public_keys[curr_client_id]

        lock = threading.Lock()
        if lock.acquire(blocking=False):
            private_key = server_new_private_keys[client_id]
            server_private_keys[client_id] = private_key
        else:
            private_key = server_private_keys[client_id]
        lock.release()

        message_blocks_encrypted = []
        message_bytes_original = message_bytes

        while len(message_bytes_original) != 0:
            block = message_bytes_original[:245]
            block_encrypted = encryptb64(block, curr_client_pbk)
            block_encoded = block_encrypted.decode('ascii')
            message_blocks_encrypted.append(block_encoded)
            message_bytes_original = message_bytes_original[245:]
        message_data = {
            'username': username,
        }
        event_type = 'chat_message_received'
        if curr_client_id == client_id:
            # ::before
            # new_key = generate_new_private_key()
            # server_private_keys[curr_client_id] = new_key
            e_private, n_private = encode_public_keynumbers(private_key.public_key().public_numbers())
            message_data['e'] = e_private
            message_data['n'] = n_private
            event_type = 'my_chat_message_received'

        server.send_message(
            curr_client,
            format_message(
                event_type,
                message_data,
                message_blocks_encrypted
            )
        )


def on_client_left_chat(client, msg):
    client_id = client['id']
    if client_id in usernames:
        username = usernames[client_id]

        server.send_message_to_all(
            format_message(
                'client_left_chat',
                {
                    'username': username
                }
            )
        )

        del usernames[client_id]
    if client_id in client_public_keys:
        del client_public_keys[client_id]
    if client_id in server_private_keys:
        del server_private_keys[client_id]


handlers = {
    "check_username": on_check_username,
    "register": on_register,
    "chat_message": on_chat_message,
    "client_left_chat": on_client_left_chat
}


def generate_keys():
    while True:
        lock = threading.Lock()
        lock.acquire()
        print("Lock acquired.")
        for client_id in usernames.keys():
            prk = generate_new_private_key()
            server_new_private_keys[client_id] = prk
        lock.release()
        print("Lock released.")
        sleep(30)


key_generator_thread = threading.Thread(target=generate_keys)
key_generator_thread.daemon = True
key_generator_thread.start()

server = WebsocketServer(5000, host='127.0.0.1', loglevel=logging.DEBUG)
server.set_fn_new_client(on_new_client)
server.set_fn_message_received(on_message)
server.set_fn_client_left(on_client_left_chat)
server.run_forever()
